namespace ProCargo.API.Configuration;

/// <summary>The "RateLimiting" section. Limits are per client IP address.</summary>
public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>Sign-in, code and registration requests per minute.</summary>
    public int AuthPermitsPerMinute { get; set; } = 10;

    /// <summary>All other requests per minute.</summary>
    public int RequestsPerMinute { get; set; } = 300;
}
