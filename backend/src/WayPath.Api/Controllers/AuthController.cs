using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WayPath.Api.Security;
using WayPath.Application;

namespace WayPath.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthenticationService service, ISecurityStore store,
    JwtSession jwt, IAntiforgery antiforgery, AuditoriaService auditoria) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("credentials")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var id = await service.Register(request, ct);
        return StatusCode(201, new { usuarioId = id, message = "Cuenta creada. Ya puede iniciar sesion." });
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("credentials")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        LoginSession session;
        try
        {
            session = await service.Login(request, HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(), ct);
        }
        catch (SecurityFault)
        {
            await AuditAccess(null, request.Email, "FALLIDO", ct);
            throw;
        }
        await AuditAccess(long.Parse(session.User.UsuarioId, System.Globalization.CultureInfo.InvariantCulture),
            session.User.Email, "EXITOSO", ct);
        jwt.Write(Response, session);
        return Ok(session.User);
    }

    [HttpGet("me"), Authorize]
    public IActionResult Me() => Ok((UserAccess)HttpContext.Items["UserAccess"]!);

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await store.EndSession(UserId, User.FindFirstValue("jti")!, ct);
        await AuditAccess(UserId, User.FindFirstValue("email"), "LOGOUT", ct);
        jwt.Clear(Response);
        return NoContent();
    }

    [HttpPut("profile"), Authorize]
    public async Task<IActionResult> Profile(ProfileRequest request, CancellationToken ct)
    {
        await service.UpdateProfile(UserId, request, ct);
        return NoContent();
    }

    [HttpPost("change-password"), Authorize, EnableRateLimiting("credentials")]
    public async Task<IActionResult> Password(PasswordRequest request, CancellationToken ct)
    {
        await service.ChangePassword(UserId, request, ct);
        jwt.Clear(Response);
        return NoContent();
    }

    private long UserId => long.Parse(User.FindFirstValue("sub")!, System.Globalization.CultureInfo.InvariantCulture);

    private async Task AuditAccess(long? userId, string? email, string result, CancellationToken ct)
    {
        try
        {
            await auditoria.RegisterAccess(userId, email, result, HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(), ct);
        }
        catch
        {
            // Authentication must not fail because the audit trail could not be written.
        }
    }
}
