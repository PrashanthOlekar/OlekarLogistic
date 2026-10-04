using System.Data.Common;

namespace ProCargo.Infrastructure.Data;

/// <summary>Opens connections to the ProCargo database.</summary>
public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
