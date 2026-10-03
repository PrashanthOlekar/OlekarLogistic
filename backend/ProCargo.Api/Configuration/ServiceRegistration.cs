using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProCargo.Api.Data;
using ProCargo.Api.Domain;
using ProCargo.Api.Services;

namespace ProCargo.Api.Configuration;

/// <summary>
/// Everything the API needs at start-up, grouped so Program.cs stays short.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>SQL Server connection. The connection string is "ProCargo" in appsettings.json.</summary>
    public static IServiceCollection AddProCargoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("ProCargo");

        services.AddDbContext<ProCargoDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));

        return services;
    }

    /// <summary>The app's own services.</summary>
    public static IServiceCollection AddProCargoServices(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddScoped<SettingsService>();
        services.AddScoped<OtpService>();
        services.AddScoped<PricingService>();
        services.AddScoped<TripService>();
        services.AddScoped<InvoiceService>();
        services.AddSingleton<TokenService>();
        services.AddSingleton<PersonalDataProtector>();
        services.AddSingleton<FileStorage>();

        // Encryption keys for PAN, bank accounts and trip codes.
        // Production: keep these in Azure Blob Storage protected by Key Vault.
        string keysFolder = Path.Combine(environment.ContentRootPath, "App_Data", "keys");
        services.AddDataProtection()
            .SetApplicationName("ProCargo")
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

        return services;
    }

    /// <summary>Sign-in tokens (JWT) and the four role-based access rules.</summary>
    public static IServiceCollection AddProCargoAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(section);

        JwtOptions jwt = section.Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Set Jwt:Key to a random secret of at least 32 characters (user-secrets or the Jwt__Key environment variable).");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                };
            });

        // Each role is also a policy, so endpoints can say .RequireAuthorization(Roles.Owner).
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Roles.Customer, policy => policy.RequireRole(Roles.Customer));
            options.AddPolicy(Roles.Owner, policy => policy.RequireRole(Roles.Owner));
            options.AddPolicy(Roles.Driver, policy => policy.RequireRole(Roles.Driver));
            options.AddPolicy(Roles.Admin, policy => policy.RequireRole(Roles.Admin));
        });

        return services;
    }

    /// <summary>Which websites may call the API (CORS), and the Swagger test page.</summary>
    public static IServiceCollection AddProCargoWeb(this IServiceCollection services, IConfiguration configuration)
    {
        string[] allowedOrigins = configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? new[] { "http://localhost:5173" };

        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "ProCargo API", Version = "v1" });

            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            };
            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearerScheme, Array.Empty<string>() } });
        });

        return services;
    }
}
