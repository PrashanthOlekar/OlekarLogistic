using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ProCargo.Application.Common.Paging;

namespace ProCargo.Infrastructure.Data;

/// <summary>
/// Runs stored procedures with Dapper. Every call uses CommandType.StoredProcedure, parameters,
/// the configured timeout and the request's cancellation token, and translates SQL errors.
/// </summary>
internal sealed class StoredProcedureExecutor(IDbConnectionFactory connections, IOptions<DatabaseSettings> settings)
{
    private readonly int _timeoutSeconds = settings.Value.CommandTimeoutSeconds;

    public Task<IReadOnlyList<T>> QueryAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(async connection =>
        {
            IEnumerable<T> rows = await connection.QueryAsync<T>(Command(procedure, parameters, cancellationToken));
            return (IReadOnlyList<T>)rows.AsList();
        }, cancellationToken);

    public Task<T?> QuerySingleOrDefaultAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(connection => connection.QueryFirstOrDefaultAsync<T>(Command(procedure, parameters, cancellationToken)), cancellationToken);

    public Task<T> ExecuteScalarAsync<T>(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(async connection =>
        {
            T? value = await connection.ExecuteScalarAsync<T>(Command(procedure, parameters, cancellationToken));
            return value ?? throw new InvalidOperationException($"{procedure} returned no value.");
        }, cancellationToken);

    public Task ExecuteAsync(string procedure, object? parameters, CancellationToken cancellationToken) =>
        RunAsync(connection => connection.ExecuteAsync(Command(procedure, parameters, cancellationToken)), cancellationToken);

    /// <summary>For procedures that return several result sets.</summary>
    public Task<T> QueryMultipleAsync<T>(
        string procedure,
        object? parameters,
        Func<SqlMapper.GridReader, Task<T>> read,
        CancellationToken cancellationToken) =>
        RunAsync(async connection =>
        {
            using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(Command(procedure, parameters, cancellationToken));
            return await read(grid);
        }, cancellationToken);

    /// <summary>For the *_GetPaged procedures: first the page of rows, then the total count.</summary>
    public Task<PagedResult<T>> QueryPagedAsync<T>(
        string procedure,
        DynamicParameters parameters,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        parameters.Add("PageNumber", page.PageNumber, DbType.Int32);
        parameters.Add("PageSize", page.PageSize, DbType.Int32);

        return QueryMultipleAsync(procedure, parameters, async grid =>
        {
            List<T> items = (await grid.ReadAsync<T>()).AsList();
            long total = await grid.ReadSingleAsync<long>();
            return PagedResult<T>.Create(items, page, total);
        }, cancellationToken);
    }

    private CommandDefinition Command(string procedure, object? parameters, CancellationToken cancellationToken) =>
        new(procedure, parameters, commandType: CommandType.StoredProcedure, commandTimeout: _timeoutSeconds, cancellationToken: cancellationToken);

    private async Task<T> RunAsync<T>(Func<DbConnection, Task<T>> work, CancellationToken cancellationToken)
    {
        try
        {
            await using DbConnection connection = await connections.OpenConnectionAsync(cancellationToken);
            return await work(connection);
        }
        catch (SqlException exception)
        {
            throw SqlErrorTranslator.Translate(exception);
        }
    }

    private async Task RunAsync(Func<DbConnection, Task> work, CancellationToken cancellationToken)
    {
        await RunAsync<bool>(async connection =>
        {
            await work(connection);
            return true;
        }, cancellationToken);
    }
}
