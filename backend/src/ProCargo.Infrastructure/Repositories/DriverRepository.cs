using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Drivers;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class DriverRepository(StoredProcedureExecutor database) : IDriverRepository
{
    public Task<Driver?> GetByIdAsync(long driverId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Driver>(StoredProcedures.DriverGetById, new { DriverId = driverId }, cancellationToken);

    public Task<Driver?> GetByUserIdAsync(long userId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Driver>(StoredProcedures.DriverGetByUserId, new { UserId = userId }, cancellationToken);

    public Task<bool> HasActiveTripAsync(long driverId, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<bool>(StoredProcedures.DriverHasActiveTrip, new { DriverId = driverId }, cancellationToken);

    public Task<PagedResult<DriverListItem>> GetPagedAsync(DriverQuery query, long? ownerId, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("OwnerId", ownerId, DbType.Int64);
        parameters.Add("KycStatus", query.KycStatus, DbType.String);
        parameters.Add("DutyStatus", query.DutyStatus, DbType.String);
        parameters.Add("Search", query.CleanSearch, DbType.String);
        parameters.Add("Sort", query.Sort ?? "Name", DbType.String);

        return database.QueryPagedAsync<DriverListItem>(StoredProcedures.DriverGetPaged, parameters, query.ToPage(), cancellationToken);
    }

    public async Task<bool> SetVerificationAsync(long driverId, bool approve, string? reason, long adminUserId, CancellationToken cancellationToken) =>
        await database.ExecuteScalarAsync<int>(
            StoredProcedures.DriverSetVerification,
            new { DriverId = driverId, Approve = approve, Reason = reason, AdminUserId = adminUserId },
            cancellationToken) > 0;
}
