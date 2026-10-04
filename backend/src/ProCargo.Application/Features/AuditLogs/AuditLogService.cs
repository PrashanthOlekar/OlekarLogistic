using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.AuditLogs;

internal sealed class AuditLogService(IAuditLogRepository auditLogs) : IAuditLogService
{
    public Task<PagedResult<AuditLogItem>> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken) =>
        auditLogs.GetPagedAsync(query, cancellationToken);
}
