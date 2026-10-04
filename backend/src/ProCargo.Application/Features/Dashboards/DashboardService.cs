using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Domain.Common;

namespace ProCargo.Application.Features.Dashboards;

internal sealed class DashboardService(IDashboardRepository dashboards, TimeProvider clock) : IDashboardService
{
    public async Task<AdminDashboard> GetAdminAsync(CancellationToken cancellationToken)
    {
        DateOnly today = IndianTime.TodayFor(clock.GetUtcNow().UtcDateTime);
        DateOnly monthStart = new(today.Year, today.Month, 1);
        DateOnly weekStart = today.AddDays(-6);

        AdminDashboard dashboard = await dashboards.GetAdminAsync(
            IndianTime.StartOfDayUtc(today),
            IndianTime.StartOfDayUtc(monthStart),
            IndianTime.StartOfDayUtc(weekStart),
            IndianTime.StartOfDayUtc(monthStart.AddMonths(-2)),
            cancellationToken);

        // Days without bookings are not in the query result; show them as zero.
        Dictionary<DateOnly, int> counts = dashboard.Last7.ToDictionary(day => day.Date, day => day.Count);
        dashboard.Last7 = Enumerable.Range(0, 7)
            .Select(offset => weekStart.AddDays(offset))
            .Select(date => new DayCount { Date = date, Count = counts.GetValueOrDefault(date) })
            .ToList();

        return dashboard;
    }
}
