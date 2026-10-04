using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);

    Task<PagedResult<AuditLogItem>> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken);
}
