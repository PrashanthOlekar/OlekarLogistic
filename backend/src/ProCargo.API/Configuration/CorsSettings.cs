namespace ProCargo.API.Configuration;

/// <summary>The "Cors" section: which websites may call the API from a browser.</summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>Exact origins, e.g. "https://portal.procargo.in". Never "*".</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
