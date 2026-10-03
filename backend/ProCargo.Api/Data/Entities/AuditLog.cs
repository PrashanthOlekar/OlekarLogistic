namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A record of who changed what, and when. Table: AuditLogs.
/// </summary>
public class AuditLog
{
    public long AuditLogId { get; set; }

    public long? ActorUserId { get; set; }

    public string Action { get; set; } = "";

    public string EntityType { get; set; } = "";

    public long EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
