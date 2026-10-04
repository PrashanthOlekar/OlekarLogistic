using System.Globalization;
using System.Security.Claims;
using ProCargo.Application.Abstractions;
using ProCargo.Infrastructure.Security;

namespace ProCargo.API.Security;

/// <summary>Reads the signed-in user from the request's JWT claims.</summary>
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public long UserId =>
        long.TryParse(Principal?.FindFirst(AppClaimTypes.UserId)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out long userId)
            ? userId
            : throw new UnauthorizedAccessException("Please sign in again.");

    public string Role => Principal?.FindFirst(AppClaimTypes.Role)?.Value ?? string.Empty;

    public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.Ordinal);

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? DeviceInfo => accessor.HttpContext?.Request.Headers.UserAgent.ToString() is { Length: > 0 } agent ? agent : null;
}
