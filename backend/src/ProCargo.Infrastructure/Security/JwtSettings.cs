using System.ComponentModel.DataAnnotations;

namespace ProCargo.Infrastructure.Security;

/// <summary>
/// The "Jwt" section of appsettings. Keep SigningKey secret: set it with user-secrets
/// or the Jwt__SigningKey environment variable, never in a committed file.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "ProCargo";

    [Required]
    public string Audience { get; set; } = "ProCargo";

    /// <summary>A random secret of at least 32 characters (HMAC-SHA256).</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Set Jwt:SigningKey (user-secrets or the Jwt__SigningKey environment variable).")]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>How long an access token works.</summary>
    [Range(5, 24 * 60)]
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>How long a refresh token works if it isn't used.</summary>
    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 14;
}
