namespace ProCargo.Application.Features.AuditLogs;

/// <summary>Records important actions (approvals, payouts, blocks...) with who did them and from where.</summary>
public interface IAuditTrail
{
    /// <param name="action">For example "Booking.Paid".</param>
    /// <param name="details">Saved as JSON. Never pass passwords, tokens or full ID numbers.</param>
    Task RecordAsync(string action, string entityType, long entityId, object? details, CancellationToken cancellationToken);
}
