using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;

namespace ProCargo.Api.Middleware;

/// <summary>
/// Turns errors into a simple JSON reply the portal can show: { "error": "message" }.
///   ApiException                → its own status code and message
///   DbUpdateConcurrencyException → 409, someone else changed the record
///   anything else                → 500 with a safe message (details go to the log)
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
        catch (ApiException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "Someone else changed this just now. Refresh and try again.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error on {Path}", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Something went wrong on our side. Please try again.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new { error = message });
    }
}
