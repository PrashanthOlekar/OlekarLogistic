using ProCargo.API.Configuration;

namespace ProCargo.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "Portal";

    /// <summary>Only the configured portal origins may call the API from a browser.</summary>
    public static IServiceCollection AddProCargoCors(this IServiceCollection services, IConfiguration configuration)
    {
        CorsSettings settings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

        if (settings.AllowedOrigins.Any(origin => origin.Trim() == "*"))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must list exact origins, not \"*\".");
        }

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(settings.AllowedOrigins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type", "Accept")
            .WithExposedHeaders("Location", "Content-Disposition")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

        return services;
    }
}
