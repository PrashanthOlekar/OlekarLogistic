using ProCargo.API.Configuration;
using ProCargo.API.ErrorHandling;
using ProCargo.API.Json;
using ProCargo.API.Security;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Features.Auth;
using ProCargo.Application.Features.Bookings;
using ProCargo.Infrastructure.Data;

namespace ProCargo.API.Extensions;

public static class ApiServiceExtensions
{
    /// <summary>Controllers, JSON, ProblemDetails, the global error handler, the current user and health checks.</summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services
            // Required fields are checked by the FluentValidation validators, which give friendlier messages.
            .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter()));

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddOptions<OtpOptions>()
            .Bind(configuration.GetSection(OtpOptions.SectionName))
            // Showing the sign-in code in the response is a testing aid: never outside Development.
            .PostConfigure(options => options.ShowCodeInResponse &= environment.IsDevelopment());

        services.AddOptions<PaymentOptions>().Bind(configuration.GetSection(PaymentOptions.SectionName));
        services.AddOptions<SeedSettings>().Bind(configuration.GetSection(SeedSettings.SectionName));

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        return services;
    }
}
