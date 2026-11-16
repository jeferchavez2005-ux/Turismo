using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WayPath.Api;

namespace WayPath.Tests;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task Unhandled_exception_is_logged_safely_and_returns_generic_problem()
    {
        var problems = new CapturingProblemDetailsService();
        var logger = new CapturingLogger<ApiExceptionHandler>();
        var handler = new ApiExceptionHandler(problems, logger, new FailingScopeFactory());
        var context = new DefaultHttpContext { TraceIdentifier = "trace-booking-500" };
        context.Response.Body = new MemoryStream();
        var exception = CaptureException();

        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("No se pudo completar la operacion.", problems.Problem?.Title);
        Assert.Null(problems.Problem?.Detail);
        Assert.Equal("trace-booking-500", problems.Problem?.Extensions["traceId"]);
        var error = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Error).Message;
        Assert.Contains("trace-booking-500", error);
        Assert.Contains(typeof(InvalidOperationException).FullName!, error);
        Assert.Contains("Booking pipeline failed", error);
        Assert.Contains(typeof(ArgumentException).FullName!, error);
        Assert.Contains("at ", error);
        Assert.DoesNotContain("secret-password", error);
        Assert.DoesNotContain("secret-token", error);
        Assert.DoesNotContain("persona@example.test", error);
        Assert.DoesNotContain("private-server", error);
        Assert.Contains("[REDACTED", error);
    }

    [Fact]
    public async Task Classified_concurrency_error_keeps_conflict_response_without_internal_details()
    {
        var problems = new CapturingProblemDetailsService();
        var logger = new CapturingLogger<ApiExceptionHandler>();
        var handler = new ApiExceptionHandler(problems, logger, new FailingScopeFactory());
        var context = new DefaultHttpContext { TraceIdentifier = "trace-conflict" };

        Assert.True(await handler.TryHandleAsync(
            context, new DbUpdateConcurrencyException("sensitive database detail"), CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("La cuenta cambio durante la operacion. Vuelva a intentarlo.", problems.Problem?.Title);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    private static Exception CaptureException()
    {
        try
        {
            throw new InvalidOperationException(
                "Booking pipeline failed password=secret-password token=secret-token persona@example.test",
                new ArgumentException("Server=private-server;Database=WayPathDB;User Id=private-user"));
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Problem { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Problem = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new InvalidOperationException("Audit unavailable in unit test");
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, formatter(state, exception)));
    }
}
