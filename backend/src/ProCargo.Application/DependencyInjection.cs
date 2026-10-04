using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProCargo.Application.Features.Approvals;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Auth;
using ProCargo.Application.Features.Bookings;
using ProCargo.Application.Features.Dashboards;
using ProCargo.Application.Features.Documents;
using ProCargo.Application.Features.Drivers;
using ProCargo.Application.Features.Loads;
using ProCargo.Application.Features.Owners;
using ProCargo.Application.Features.Payments;
using ProCargo.Application.Features.Pricing;
using ProCargo.Application.Features.Profiles;
using ProCargo.Application.Features.ReferenceData;
using ProCargo.Application.Features.Registration;
using ProCargo.Application.Features.Settlements;
using ProCargo.Application.Features.Trips;
using ProCargo.Application.Features.Users;
using ProCargo.Application.Features.Vehicles;

namespace ProCargo.Application;

public static class DependencyInjection
{
    /// <summary>Registers every application service and validator.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton, includeInternalTypes: true);

        // Shared helpers (scoped: they cache per request)
        services.AddScoped<IBusinessSettingsProvider, BusinessSettingsProvider>();
        services.AddScoped<IQuotePricer, QuotePricer>();
        services.AddScoped<ICurrentProfiles, CurrentProfiles>();
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ISessionIssuer, SessionIssuer>();

        // Feature services, one per area
        services.AddScoped<IReferenceDataService, ReferenceDataService>();
        services.AddScoped<IPriceEstimateService, PriceEstimateService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ILoadService, LoadService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
