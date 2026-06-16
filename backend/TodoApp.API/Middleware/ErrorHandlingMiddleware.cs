using System.Net;
using System.Text.Json;

namespace TodoApp.API.Middleware;

/// <summary>
/// Global exception handler. Catches any unhandled exception, assigns it a
/// unique error ID, logs the full details server-side, and returns only the
/// opaque error ID to the caller.
///
/// This prevents stack traces, SQL errors, and internal paths from leaking to
/// clients while still giving operators a token they can grep in the logs.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Short, memorable token: ERR- + 8 hex chars from a fresh GUID.
        // Keeps it copy-pasteable without being a full UUID in the UI.
        var errorId = $"ERR-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        // Log the full exception server-side, keyed on the error ID.
        _logger.LogError(
            ex,
            "Unhandled exception [{ErrorId}] {Method} {Path}: {Message}",
            errorId,
            context.Request.Method,
            context.Request.Path,
            ex.Message);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(
            new ErrorResponse(errorId, "An unexpected error occurred."),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}

/// <summary>Response shape returned to clients on unhandled errors.</summary>
public record ErrorResponse(string ErrorId, string Message);
