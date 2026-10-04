namespace ProCargo.Domain.Constants;

/// <summary>TripEvents.EventType: the timeline the customer sees.</summary>
public static class TripEventType
{
    public const string Assigned = "Assigned";
    public const string EnRouteToPickup = "EnRouteToPickup";
    public const string ReachedPickup = "ReachedPickup";
    public const string PickupOtpVerified = "PickupOtpVerified";
    public const string GoodsPhotoUploaded = "GoodsPhotoUploaded";
    public const string TripStarted = "TripStarted";
    public const string ReachedDestination = "ReachedDestination";
    public const string PodUploaded = "PodUploaded";
    public const string DeliveryOtpVerified = "DeliveryOtpVerified";
    public const string PodApproved = "PodApproved";
    public const string Cancelled = "Cancelled";
}
