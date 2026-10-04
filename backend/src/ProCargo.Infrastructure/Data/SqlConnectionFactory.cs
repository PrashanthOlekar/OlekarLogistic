using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ProCargo.Infrastructure.Data;

/// <summary>Singleton. Each call opens a new pooled SqlConnection; the caller disposes it.</summary>
internal sealed class SqlConnectionFactory(IOptions<DatabaseSettings> settings) : IDbConnectionFactory
{
    private readonly string _connectionString = settings.Value.ConnectionString;

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
