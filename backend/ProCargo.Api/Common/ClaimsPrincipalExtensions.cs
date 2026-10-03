using System.Security.Claims;

namespace ProCargo.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's Users.UserId, read from the sign-in token.</summary>
    public static long GetUserId(this ClaimsPrincipal user)
    {
        string? value = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(value, out long userId))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Please sign in again.");
        }

        return userId;
    }
}
