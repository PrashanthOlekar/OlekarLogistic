namespace ProCargo.Domain.Constants;

/// <summary>
/// Trips.Status, in the order the driver moves through them:
/// Assigned → EnRouteToPickup → AtPickup → Loaded → InTransit → AtDestination → Delivered → Completed.
/// </summary>
public static class TripStatus
{
    public const string Assigned = "Assigned";
    public const string EnRouteToPickup = "EnRouteToPickup";
    public const string AtPickup = "AtPickup";
    public const string Loaded = "Loaded";
    public const string InTransit = "InTransit";
    public const string AtDestination = "AtDestination";
    public const string Delivered = "Delivered";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
        [Assigned, EnRouteToPickup, AtPickup, Loaded, InTransit, AtDestination, Delivered, Completed, Cancelled];

    /// <summary>A trip in one of these states no longer keeps its truck and driver busy.</summary>
    public static readonly IReadOnlyList<string> Finished = [Delivered, Completed, Cancelled];

    /// <summary>Goods are not on the truck yet, so the booking can still be cancelled.</summary>
    public static bool IsBeforeLoading(string status) => status is Assigned or EnRouteToPickup or AtPickup;
}
