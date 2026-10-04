using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class AuditLogRepository(StoredProcedureExecutor database) : IAuditLogRepository
{
    public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.AuditLogCreate, new
        {
            entry.ActorUserId,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.NewValues,
            entry.IpAddress,
        }, cancellationToken);

    public Task<PagedResult<AuditLogItem>> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("EntityType", query.EntityType, DbType.String);
        parameters.Add("Search", query.CleanSearch, DbType.String);

        return database.QueryPagedAsync<AuditLogItem>(StoredProcedures.AuditLogGetPaged, parameters, query.ToPage(), cancellationToken);
    }
}
