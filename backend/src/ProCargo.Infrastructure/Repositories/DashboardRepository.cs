using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Features.Dashboards;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class DashboardRepository(StoredProcedureExecutor database) : IDashboardRepository
{
    public Task<AdminDashboard> GetAdminAsync(
        DateTime todayStartUtc,
        DateTime monthStartUtc,
        DateTime weekStartUtc,
        DateTime routesSinceUtc,
        CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(
            StoredProcedures.DashboardGetAdmin,
            new
            {
                TodayStartAt = todayStartUtc,
                MonthStartAt = monthStartUtc,
                WeekStartAt = weekStartUtc,
                RoutesSinceAt = routesSinceUtc,
            },
            async grid =>
            {
                AdminDashboard dashboard = await grid.ReadSingleAsync<AdminDashboard>();
                dashboard.Last7 = (await grid.ReadAsync<DayCount>()).ToList();
                dashboard.Routes = (await grid.ReadAsync<RouteCount>()).ToList();
                dashboard.Cities = (await grid.ReadAsync<CityCount>()).ToList();
                dashboard.Vehicles = (await grid.ReadAsync<StatusCount>()).ToList();
                return dashboard;
            },
            cancellationToken);
}
