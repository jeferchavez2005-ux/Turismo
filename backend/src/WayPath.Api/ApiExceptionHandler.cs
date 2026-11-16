using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;

namespace WayPath.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger,
    IServiceScopeFactory scopes)
    : IExceptionHandler
{
    private const int MaxDiagnosticLength = 4096;
    private static readonly Regex SensitiveAssignment = new(
        "(?i)\\b(password|passwd|pwd|contrasena|contraseña|token|access_token|refresh_token|authorization|cookie|connection(?:\\s*string)?)\\b\\s*[:=]\\s*(?:\\\"[^\\\"]*\\\"|'[^']*'|[^;,\\s}]+)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex ConnectionString = new(
        "(?i)\\b(server|data source|initial catalog|database|user id|uid)\\s*=.*(?:;|$)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex BearerToken = new(
        "(?i)\\bbearer\\s+[a-z0-9._~+/-]+=*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex JwtToken = new(
        "\\beyJ[a-zA-Z0-9_-]+\\.eyJ[a-zA-Z0-9_-]+\\.[a-zA-Z0-9_-]+\\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex EmailAddress = new(
        "\\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}\\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, isUnhandled) = exception switch
        {
            SecurityFault fault => (fault.Status, fault.Message, false),
            DbUpdateConcurrencyException => (409, "La cuenta cambio durante la operacion. Vuelva a intentarlo.", false),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                (409, "Ya existe un registro con esos datos.", false),
            SqlException { Number: 1205 } => (409, "Operacion concurrente. Vuelva a intentarlo.", false),
            DbUpdateException { InnerException: SqlException { Number: 1205 } } =>
                (409, "Operacion concurrente. Vuelva a intentarlo.", false),
            SqlException => (503, "No se pudo acceder a WayPathDB.", false),
            _ => (500, "No se pudo completar la operacion.", true)
        };
        if (isUnhandled)
            LogUnhandled(context.TraceIdentifier, exception);
        else if (status >= 500)
            logger.LogError("Request {TraceId} failed: {ExceptionType}",
                context.TraceIdentifier, exception.GetType().Name);
        if (status >= 500)
            await AuditError(context, exception, status, ct);
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context, ProblemDetails = new ProblemDetails
            {
                Status = status, Title = title, Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
        return true;
    }

    private void LogUnhandled(string traceId, Exception exception)
    {
        var inner = exception.InnerException;
        logger.LogError(
            "Unhandled request {TraceId}: {ExceptionType}; Message={ExceptionMessage}; " +
            "InnerType={InnerExceptionType}; InnerMessage={InnerExceptionMessage}; StackTrace={StackTrace}",
            traceId,
            exception.GetType().FullName,
            SafeDiagnostic(exception.Message),
            inner?.GetType().FullName ?? "none",
            SafeDiagnostic(inner?.Message),
            SafeDiagnostic(exception.StackTrace));
    }

    private static string SafeDiagnostic(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "none";
        var safe = value.Replace('\r', ' ').Replace('\n', ' ');
        safe = ConnectionString.Replace(safe, "[REDACTED_CONNECTION_STRING]");
        safe = SensitiveAssignment.Replace(safe, "$1=[REDACTED]");
        safe = BearerToken.Replace(safe, "Bearer [REDACTED]");
        safe = JwtToken.Replace(safe, "[REDACTED_TOKEN]");
        safe = EmailAddress.Replace(safe, "[REDACTED_EMAIL]");
        return safe.Length <= MaxDiagnosticLength ? safe : safe[..MaxDiagnosticLength] + "[TRUNCATED]";
    }

    private async Task AuditError(HttpContext context, Exception exception, int status, CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var audit = scope.ServiceProvider.GetRequiredService<AuditoriaService>();
            var userId = long.TryParse(context.User.FindFirstValue("sub"), out var id) ? id : (long?)null;
            var data = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                traceId = context.TraceIdentifier,
                path = context.Request.Path.Value,
                method = context.Request.Method,
                status
            });
            await audit.RegisterError(userId, status >= 500 ? "ERROR" : "WARN",
                exception.GetType().Name, "No se pudo completar la operacion.",
                exception.StackTrace, data, ct);
        }
        catch (Exception auditException)
        {
            logger.LogWarning("Audit error registration failed: {ExceptionType}", auditException.GetType().Name);
        }
    }
}
