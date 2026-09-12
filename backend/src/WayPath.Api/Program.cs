using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using WayPath.Api;
using WayPath.Api.Security;
using WayPath.Application;
using WayPath.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32 * 1024);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
// Environment variables remain the final authority for deployment settings.
builder.Configuration.AddEnvironmentVariables();
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new();
byte[] signingKey;
try { signingKey = Convert.FromBase64String(jwt.SigningKey); }
catch (FormatException) { throw new InvalidOperationException("Jwt:SigningKey debe ser una clave aleatoria Base64 de al menos 32 bytes."); }
if (signingKey.Length < 32 || jwt.LifetimeMinutes is < 1 or > 30 ||
    string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
    throw new InvalidOperationException("Configure Jwt:SigningKey (32+ bytes Base64), Issuer, Audience y vigencia de 1 a 30 minutos.");
var connection = builder.Configuration.GetConnectionString("WayPathDB");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Configure ConnectionStrings:WayPathDB fuera del codigo.");

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<JwtSession>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<OperacionService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<PagoService>();
builder.Services.AddScoped<VentaService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddScoped<ISecurityStore, SecurityStore>();
builder.Services.AddScoped<IClienteStore, ClienteStore>();
builder.Services.AddScoped<ICatalogoStore, CatalogoStore>();
builder.Services.AddScoped<IOperacionStore, OperacionStore>();
builder.Services.AddScoped<IReservaStore, ReservaStore>();
builder.Services.AddScoped<IPagoStore, PagoStore>();
builder.Services.AddScoped<IVentaStore, VentaStore>();
builder.Services.AddScoped<IDashboardStore, DashboardStore>();
builder.Services.AddScoped<IReporteStore, ReporteStore>();
builder.Services.AddDbContext<WayPathDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddControllers(options => options.Filters.Add<CsrfFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.UnmappedMemberHandling =
        System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "WayPath.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(signingKey),
        ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.FromSeconds(15), RoleClaimType = "role", NameClaimType = "sub"
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
                context.Token = context.Request.Cookies[JwtSession.CookieName];
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var id = context.Principal?.FindFirstValue("sub");
            var sid = context.Principal?.FindFirstValue("jti");
            if (!long.TryParse(id, out var userId) || string.IsNullOrEmpty(sid))
            {
                context.Fail("Invalid session");
                return;
            }
            var store = context.HttpContext.RequestServices.GetRequiredService<ISecurityStore>();
            var access = await store.GetAccess(userId, sid, context.HttpContext.RequestAborted);
            if (access is null) { context.Fail("Inactive session"); return; }
            // Ignore any role/permission claims from the token; use current database grants.
            var identity = new ClaimsIdentity("WayPath", "sub", "role");
            identity.AddClaim(new("sub", id!));
            identity.AddClaim(new("jti", sid));
            identity.AddClaims(access.Roles.Select(r => new Claim("role", r)));
            identity.AddClaims(access.Permisos.Select(p => new Claim("permission", p)));
            context.Principal = new ClaimsPrincipal(identity);
            context.HttpContext.Items["UserAccess"] = access;
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("security.manage", policy => policy.RequireAuthenticatedUser()
        .RequireRole("ADMINISTRADOR").RequireClaim("permission", "SEGURIDAD_USUARIOS_GESTIONAR"));
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("credentials", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
        }));
});

var app = builder.Build();
if (args.Contains("--create-admin", StringComparer.Ordinal))
{
    await BootstrapAdmin.Run(app.Services);
    return;
}
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'none'";
    if (context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.CacheControl = "no-store";
    await next();
});
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
    app.Use(async (context, next) =>
    {
        if (!context.Request.IsHttps)
        {
            await Results.Problem(statusCode: 400, title: "HTTPS es obligatorio.").ExecuteAsync(context);
            return;
        }
        await next();
    });
}
var frontend = app.Environment.WebRootPath;
var sourceFrontend = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../../../Proyecto_Turismo"));
if (app.Environment.IsDevelopment() && Directory.Exists(sourceFrontend))
    frontend = sourceFrontend;
if (Directory.Exists(frontend))
{
    var provider = new PhysicalFileProvider(frontend);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
}
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/ready", async (ISecurityStore store, CancellationToken ct) =>
{
    try { return await store.CanConnect(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503); }
    catch (Exception) { return Results.StatusCode(503); }
}).AllowAnonymous();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
await app.RunAsync();

public partial class Program { }
