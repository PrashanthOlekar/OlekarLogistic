using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.AuditLogs;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogItem>> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken);
}
