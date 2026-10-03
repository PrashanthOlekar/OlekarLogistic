namespace ProCargo.Api.Domain;

// Every status code used in the database, in one place.
// The values must match the CHECK constraints in database/ProCargo.sql.

/// <summary>Users.Status</summary>
public static class UserStatus
{
    public const string Active = "Active";
    public const string PendingKyc = "PendingKyc";
    public const string Blocked = "Blocked";
    public const string Closed = "Closed";
}

/// <summary>Owners.KycStatus, Drivers.KycStatus</summary>
public static class KycStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

/// <summary>Drivers.DutyStatus</summary>
public static class DutyStatus
{
    public const string Available = "Available";
    public const string OnTrip = "OnTrip";
    public const string OffDuty = "OffDuty";
}

/// <summary>Vehicles.AvailabilityStatus — set by the owner.</summary>
public static class VehicleAvailability
{
    public const string Available = "Available";
    public const string Busy = "Busy";
    public const string Maintenance = "Maintenance";
}

/// <summary>Vehicles.VerificationStatus — set by an admin.</summary>
public static class VehicleVerification
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Suspended = "Suspended";
}

/// <summary>Documents.Status</summary>
public static class DocumentStatus
{
    public const string Pending = "Pending";
    public const string Verified = "Verified";
    public const string Rejected = "Rejected";
    public const string Expired = "Expired";
}

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
}

/// <summary>Quotes.Status</summary>
public static class QuoteStatus
{
    public const string Sent = "Sent";
    public const string Accepted = "Accepted";
    public const string Expired = "Expired";
    public const string Superseded = "Superseded";
}

/// <summary>LoadOffers.Status</summary>
public static class LoadOfferStatus
{
    public const string Offered = "Offered";
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";
}

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

    /// <summary>A trip in one of these states no longer keeps its truck and driver busy.</summary>
    public static readonly string[] Finished = { Delivered, Completed, Cancelled };
}

/// <summary>TripEvents.EventType — the timeline the customer sees.</summary>
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

/// <summary>Payments.Status</summary>
public static class PaymentStatus
{
    public const string Created = "Created";
    public const string Captured = "Captured";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
}

/// <summary>
/// Settlements.Status, in order:
/// AwaitingPod → Approved (POD checked) → Released (money sent to the owner).
/// </summary>
public static class SettlementStatus
{
    public const string AwaitingPod = "AwaitingPod";
    public const string Approved = "Approved";
    public const string Released = "Released";
    public const string OnHold = "OnHold";
}

/// <summary>OtpCodes.Purpose</summary>
public static class OtpPurpose
{
    public const string Login = "Login";
    public const string Signup = "Signup";
}
