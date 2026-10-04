namespace ProCargo.Application.Features.AuditLogs;

/// <summary>One row for the AuditLogs table.</summary>
/// <param name="NewValues">Details as JSON, or null.</param>
public sealed record AuditEntry(long? ActorUserId, string Action, string EntityType, long EntityId, string? NewValues, string? IpAddress);
