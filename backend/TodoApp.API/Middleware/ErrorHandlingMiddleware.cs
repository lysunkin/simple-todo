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

        int statusCode;
        string message;

        if (ex is ArgumentException)
        {
            // Validation errors thrown by the service layer (e.g. invalid priority value).
            statusCode = (int)HttpStatusCode.BadRequest;
            message = ex.Message;
            _logger.LogWarning(
                "Bad request [{ErrorId}] {Method} {Path}: {Message}",
                errorId,
                context.Request.Method,
                context.Request.Path,
                ex.Message);
        }
        else
        {
            // Unexpected errors: log full exception, return opaque error ID to caller.
            statusCode = (int)HttpStatusCode.InternalServerError;
            message = "An unexpected error occurred.";
            _logger.LogError(
                ex,
                "Unhandled exception [{ErrorId}] {Method} {Path}: {Message}",
                errorId,
                context.Request.Method,
                context.Request.Path,
                ex.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(
            new ErrorResponse(errorId, message),
            _jsonOptions);

        await context.Response.WriteAsync(body);
    }

    // Allocated once to avoid per-request allocations.
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}

/// <summary>Response shape returned to clients on unhandled errors.</summary>
public record ErrorResponse(string ErrorId, string Message);
