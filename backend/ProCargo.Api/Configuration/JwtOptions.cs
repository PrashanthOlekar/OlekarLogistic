namespace ProCargo.Api.Configuration;

/// <summary>
/// Sign-in token settings, read from the "Jwt" section of appsettings.json.
/// Keep Key secret: set it with user-secrets or the Jwt__Key environment variable.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ProCargo";

    public string Audience { get; set; } = "ProCargo";

    /// <summary>A random secret of at least 32 characters.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenHours { get; set; } = 12;
}
