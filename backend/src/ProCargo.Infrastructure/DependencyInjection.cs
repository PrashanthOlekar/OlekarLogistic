using Dapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProCargo.Application.Abstractions.Messaging;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Abstractions.Storage;
using ProCargo.Infrastructure.Data;
using ProCargo.Infrastructure.Messaging;
using ProCargo.Infrastructure.Repositories;
using ProCargo.Infrastructure.Security;
using ProCargo.Infrastructure.Storage;

namespace ProCargo.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the database, repositories, security and storage. Settings are checked at start-up.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection(DatabaseSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<StorageSettings>()
            .Bind(configuration.GetSection(StorageSettings.SectionName));

        // Database
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<StoredProcedureExecutor>();

        // Repositories: one per area, each calling its stored procedures
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ILoadRepository, LoadRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ISettlementRepository, SettlementRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        // Security
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPersonalDataProtector, PersonalDataProtector>();

        // Encryption keys for PAN, bank accounts and trip codes.
        // Production: keep these in Azure Blob Storage protected by Key Vault.
        string keysFolder = Path.Combine(
            environment.ContentRootPath,
            configuration[$"{StorageSettings.SectionName}:KeysFolder"] ?? "App_Data/keys");
        services.AddDataProtection()
            .SetApplicationName("ProCargo")
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

        // Files and messages
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<ISmsSender, LoggingSmsSender>();

        return services;
    }
}
