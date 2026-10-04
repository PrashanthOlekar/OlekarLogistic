using System.Text.Json;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;

namespace ProCargo.Application.Features.AuditLogs;

internal sealed class AuditTrail(IAuditLogRepository auditLogs, ICurrentUser currentUser) : IAuditTrail
{
    public Task RecordAsync(string action, string entityType, long entityId, object? details, CancellationToken cancellationToken)
    {
        var entry = new AuditEntry(
            currentUser.IsAuthenticated ? currentUser.UserId : null,
            action,
            entityType,
            entityId,
            details is null ? null : JsonSerializer.Serialize(details),
            currentUser.IpAddress);

        return auditLogs.AddAsync(entry, cancellationToken);
    }
}
