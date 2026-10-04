using ProCargo.Application.Features.Dashboards;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IDashboardRepository
{
    /// <summary>Counters and chart data. All times are UTC.</summary>
    Task<AdminDashboard> GetAdminAsync(
        DateTime todayStartUtc,
        DateTime monthStartUtc,
        DateTime weekStartUtc,
        DateTime routesSinceUtc,
        CancellationToken cancellationToken);
}
