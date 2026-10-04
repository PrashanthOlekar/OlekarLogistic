using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProCargo.Application.Common.Exceptions;

namespace ProCargo.API.ErrorHandling;

/// <summary>
/// Turns every exception into a ProblemDetails response, so controllers need no try/catch.
/// Messages from the application's own exceptions are safe to show; anything else becomes a
/// generic 500 and the details go only to the log (no SQL, connection strings or stack traces).
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The browser went away; nobody is waiting for an answer.
            httpContext.Response.StatusCode = 499;
            return true;
        }

        ProblemDetails problem = exception switch
        {
            ValidationException validation => ValidationProblem(validation),
            BusinessRuleException => Problem(StatusCodes.Status400BadRequest, "Request not allowed", ProblemTypes.BusinessRule, exception.Message),
            UnauthorizedAccessException => Problem(StatusCodes.Status401Unauthorized, "Sign-in required", ProblemTypes.Unauthorized, exception.Message),
            ForbiddenException => Problem(StatusCodes.Status403Forbidden, "Not allowed", ProblemTypes.Forbidden, exception.Message),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "Not found", ProblemTypes.NotFound, exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Conflict", ProblemTypes.Conflict, exception.Message),
            TooManyRequestsException => Problem(StatusCodes.Status429TooManyRequests, "Too many requests", ProblemTypes.TooManyRequests, exception.Message),
            FeatureUnavailableException => Problem(StatusCodes.Status501NotImplemented, "Not available yet", ProblemTypes.NotImplemented, exception.Message),
            _ => Problem(StatusCodes.Status500InternalServerError, "Server error", ProblemTypes.ServerError, "Something went wrong on our side. Please try again."),
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled error on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("{Status} on {Method} {Path}: {Message}", problem.Status, httpContext.Request.Method, httpContext.Request.Path, problem.Detail);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string type, string detail) => new()
    {
        Status = status,
        Title = title,
        Type = type,
        Detail = detail,
    };

    /// <summary>400 with every failed field: { "errors": { "weightKg": ["Enter the approximate weight in kg."] } }</summary>
    private static ValidationProblemDetails ValidationProblem(ValidationException exception)
    {
        Dictionary<string, string[]> errors = exception.Errors
            .GroupBy(error => JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName ?? string.Empty))
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Type = ProblemTypes.Validation,
            Detail = exception.Errors.Select(error => error.ErrorMessage).FirstOrDefault() ?? "Check the highlighted fields.",
        };
    }
}
