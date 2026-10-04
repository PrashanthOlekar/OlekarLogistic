using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.AuditLogs;

/// <summary>Filters for GET /audit-logs.</summary>
public sealed class AuditLogQuery : ListQuery
{
    /// <summary>For example Booking, Trip, Settlement, User.</summary>
    public string? EntityType { get; set; }
}
