using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Settlements;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class SettlementRepository(StoredProcedureExecutor database) : ISettlementRepository
{
    public Task<Settlement?> GetByIdAsync(long settlementId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Settlement>(StoredProcedures.SettlementGetById, new { SettlementId = settlementId }, cancellationToken);

    public Task<PagedResult<SettlementListItem>> GetPagedAsync(SettlementQuery query, long? ownerId, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("OwnerId", ownerId, DbType.Int64);
        parameters.Add("Status", query.Status, DbType.String);
        parameters.Add("Search", query.CleanSearch, DbType.String);

        return database.QueryPagedAsync<SettlementListItem>(StoredProcedures.SettlementGetPaged, parameters, query.ToPage(), cancellationToken);
    }

    public Task RecordPayoutAsync(long settlementId, string utr, long releasedBy, CancellationToken cancellationToken) =>
        database.ExecuteAsync(
            StoredProcedures.SettlementRecordPayout,
            new { SettlementId = settlementId, Utr = utr, ReleasedBy = releasedBy },
            cancellationToken);
}
