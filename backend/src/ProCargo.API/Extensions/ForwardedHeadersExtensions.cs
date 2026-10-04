using Microsoft.AspNetCore.HttpOverrides;
using ProCargo.API.Configuration;

namespace ProCargo.API.Extensions;

public static class ForwardedHeadersExtensions
{
    /// <summary>Trusts X-Forwarded-For/-Proto only from the configured proxy networks (and loopback).</summary>
    public static IServiceCollection AddProCargoForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        ReverseProxySettings settings = configuration.GetSection(ReverseProxySettings.SectionName).Get<ReverseProxySettings>() ?? new();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            foreach (string network in settings.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }
}
