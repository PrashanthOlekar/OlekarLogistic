using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ProCargo.API.Configuration;
using ProCargo.API.ErrorHandling;

namespace ProCargo.API.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>Per-IP request limits: a general one, and a stricter one for sign-in and registration.</summary>
    public static IServiceCollection AddProCargoRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitSettings>().Bind(configuration.GetSection(RateLimitSettings.SectionName));

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Settings(context).RequestsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                }));

            options.AddPolicy(RateLimitPolicies.Auth, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Settings(context).AuthPermitsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                IProblemDetailsService problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Type = ProblemTypes.TooManyRequests,
                        Detail = "Too many requests. Wait a minute and try again.",
                    },
                });
            };
        });

        return services;
    }

    private static RateLimitSettings Settings(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;

    /// <summary>Behind a proxy, enable forwarded headers so this is the real client address.</summary>
    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
