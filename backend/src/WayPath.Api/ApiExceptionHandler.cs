using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;

namespace WayPath.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            SecurityFault fault => (fault.Status, fault.Message),
            DbUpdateConcurrencyException => (409, "La cuenta cambio durante la operacion. Vuelva a intentarlo."),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                (409, "Ya existe un registro con esos datos."),
            SqlException { Number: 1205 } => (409, "Operacion concurrente. Vuelva a intentarlo."),
            DbUpdateException { InnerException: SqlException { Number: 1205 } } =>
                (409, "Operacion concurrente. Vuelva a intentarlo."),
            SqlException => (503, "No se pudo acceder a WayPathDB."),
            _ => (500, "No se pudo completar la operacion.")
        };
        // Do not log exception messages: SQL errors can contain user data.
        if (status >= 500) logger.LogError("Request {TraceId} failed: {ExceptionType}",
            context.TraceIdentifier, exception.GetType().Name);
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
}

