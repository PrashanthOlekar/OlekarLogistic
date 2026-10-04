namespace ProCargo.Application.Features.AuditLogs;

public sealed class AuditLogItem
{
    public long Id { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public long EntityId { get; set; }

    public long? ActorUserId { get; set; }

    public string? ActorName { get; set; }

    /// <summary>Details as JSON.</summary>
    public string? NewValues { get; set; }

    public DateTime CreatedAt { get; set; }
}
