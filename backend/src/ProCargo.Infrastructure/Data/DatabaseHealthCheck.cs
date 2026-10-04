using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProCargo.Infrastructure.Data;

/// <summary>GET /health reports Unhealthy when SQL Server can't be reached.</summary>
public sealed class DatabaseHealthCheck(IDbConnectionFactory connections) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using DbConnection connection = await connections.OpenConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Cannot reach SQL Server.", exception);
        }
    }
}
