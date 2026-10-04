namespace ProCargo.Infrastructure.Security;

/// <summary>Short JWT claim names. The API reads them with MapInboundClaims = false.</summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Name = "name";
    public const string Role = "role";
}
