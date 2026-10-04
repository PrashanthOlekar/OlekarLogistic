namespace ProCargo.Application.Features.Dashboards;

public interface IDashboardService
{
    Task<AdminDashboard> GetAdminAsync(CancellationToken cancellationToken);
}
