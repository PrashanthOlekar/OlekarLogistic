namespace ProCargo.Domain.Constants;

/// <summary>
/// Bookings.Status, in the order a booking moves through them:
/// Quoted → Confirmed (paid) → Assigned → InTransit → Delivered → Completed.
/// </summary>
public static class BookingStatus
{
    public const string QuotePending = "QuotePending";
    public const string Quoted = "Quoted";
    public const string Confirmed = "Confirmed";
    public const string Assigned = "Assigned";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
        [QuotePending, Quoted, Confirmed, Assigned, InTransit, Delivered, Completed, Cancelled];

    /// <summary>A customer can cancel online until the goods are loaded.</summary>
    public static bool CanBeCancelled(string status) => status is QuotePending or Quoted or Confirmed or Assigned;
}
