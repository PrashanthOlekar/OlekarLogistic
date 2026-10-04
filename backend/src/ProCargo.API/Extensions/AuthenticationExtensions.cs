using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProCargo.Domain.Constants;
using ProCargo.Infrastructure.Security;

namespace ProCargo.API.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>JWT bearer sign-in and the role-based policies. Every endpoint needs a signed-in user unless marked [AllowAnonymous].</summary>
    public static IServiceCollection AddProCargoAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Read the validated Jwt settings when the options are first needed.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                JwtSettings jwt = jwtOptions.Value;

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AppClaimTypes.Name,
                    RoleClaimType = AppClaimTypes.Role,
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.CustomerOnly, policy => policy.RequireRole(Roles.Customer))
            .AddPolicy(Policies.OwnerOnly, policy => policy.RequireRole(Roles.Owner))
            .AddPolicy(Policies.DriverOnly, policy => policy.RequireRole(Roles.Driver))
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.CustomerOrAdmin, policy => policy.RequireRole(Roles.Customer, Roles.Admin))
            .AddPolicy(Policies.OwnerOrAdmin, policy => policy.RequireRole(Roles.Owner, Roles.Admin))
            .AddPolicy(Policies.OwnerDriverOrAdmin, policy => policy.RequireRole(Roles.Owner, Roles.Driver, Roles.Admin))
            .AddPolicy(Policies.AccountHolder, policy => policy.RequireRole(Roles.Customer, Roles.Owner, Roles.Driver));

        return services;
    }
}
