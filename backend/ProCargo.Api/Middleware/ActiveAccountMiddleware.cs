using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Data;
using ProCargo.Api.Domain;

namespace ProCargo.Api.Middleware;

/// <summary>
/// Stops blocked or closed accounts straight away, even if their sign-in token is still valid.
/// Runs after authentication, before authorization.
/// </summary>
public class ActiveAccountMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveAccountMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ProCargoDbContext db)
    {
        bool isSignedIn = context.User.Identity?.IsAuthenticated == true;
        string? userIdText = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (isSignedIn && long.TryParse(userIdText, out long userId))
        {
            string? status = await db.Users
                .Where(user => user.UserId == userId)
                .Select(user => user.Status)
                .FirstOrDefaultAsync();

            if (status is null or UserStatus.Blocked or UserStatus.Closed)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "This account is not active. Contact ProCargo support." });
                return;
            }
        }

        await _next(context);
    }
}
