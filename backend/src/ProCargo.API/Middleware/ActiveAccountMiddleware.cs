using Microsoft.AspNetCore.Mvc;
using ProCargo.API.ErrorHandling;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.API.Middleware;

/// <summary>
/// Stops blocked or closed accounts straight away, even if their access token is still valid.
/// Runs after authentication, before authorization.
/// </summary>
internal sealed class ActiveAccountMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUser currentUser,
        IUserRepository users,
        IProblemDetailsService problemDetails)
    {
        if (currentUser.IsAuthenticated)
        {
            User? user = await users.GetByIdAsync(currentUser.UserId, context.RequestAborted);
            if (user is null || UserStatus.IsLockedOut(user.Status))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Account not active",
                        Type = ProblemTypes.Forbidden,
                        Detail = "This account is not active. Contact ProCargo support.",
                    },
                });
                return;
            }
        }

        await next(context);
    }
}
