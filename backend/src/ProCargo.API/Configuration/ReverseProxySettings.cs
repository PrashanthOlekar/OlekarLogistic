namespace ProCargo.API.Configuration;

/// <summary>
/// The "ReverseProxy" section. When the API runs behind nginx or a load balancer, list the proxy's
/// network here so the client's real IP (X-Forwarded-For) is used for rate limiting and audit logs.
/// Headers from any other address are ignored, so clients can't fake their IP.
/// </summary>
public sealed class ReverseProxySettings
{
    public const string SectionName = "ReverseProxy";

    /// <summary>Networks in CIDR form, e.g. "172.16.0.0/12" for Docker. Empty: only a proxy on the same machine is trusted.</summary>
    public string[] KnownNetworks { get; set; } = [];
}
