using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using WayPath.Application;

namespace WayPath.Api;

public sealed class AuditChangeFilter(AuditoriaService auditoria, ILogger<AuditChangeFilter> logger)
    : IAsyncActionFilter
{
    private static readonly HashSet<string> AuditedRoots =
        ["security", "clientes", "catalogo", "operacion", "reservas", "pagos", "ventas", "reportes"];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        var request = context.HttpContext.Request;
        if (executed.Exception is not null || !ShouldAudit(request)) return;

        var route = request.Path.Value?.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (route.Length < 2) return;
        var root = route[1].ToLowerInvariant();
        if (!AuditedRoots.Contains(root)) return;

        var userId = long.TryParse(context.HttpContext.User.FindFirstValue("sub"), out var parsed) ? parsed : (long?)null;
        var operation = request.Method switch
        {
            "POST" => "INSERT",
            "PUT" or "PATCH" => "UPDATE",
            "DELETE" => "DELETE",
            _ => null
        };
        if (operation is null) return;

        var summary = JsonSerializer.SerializeToElement(new
        {
            method = request.Method,
            path = request.Path.Value,
            route = string.Join("/", route.Skip(1)),
            status = context.HttpContext.Response.StatusCode
        });
        try
        {
            await auditoria.RegisterChange(userId, SchemaFor(root), TableFor(route), KeyFor(route), operation,
                null, summary, context.HttpContext.Connection.RemoteIpAddress?.ToString(), context.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Audit change registration failed: {ExceptionType}", ex.GetType().Name);
        }
    }

    private static bool ShouldAudit(HttpRequest request) =>
        request.Path.StartsWithSegments("/api") &&
        request.Method is "POST" or "PUT" or "PATCH" or "DELETE" &&
        !request.Path.StartsWithSegments("/api/auth/login") &&
        !request.Path.StartsWithSegments("/api/auth/csrf") &&
        !request.Path.StartsWithSegments("/api/auditoria");

    private static string SchemaFor(string root) => root switch
    {
        "security" => "Seguridad",
        "clientes" => "Cliente",
        "catalogo" => "Catalogo",
        "operacion" => "Operacion",
        "reservas" or "ventas" => "Ventas",
        "pagos" => "Pagos",
        "reportes" => "Reportes",
        _ => "Aplicacion"
    };

    private static string TableFor(string[] route) => route.Length >= 3 ? route[2] : route[^1];
    private static string KeyFor(string[] route) => route.Reverse().FirstOrDefault(x => long.TryParse(x, out _)) ?? "request";
}
