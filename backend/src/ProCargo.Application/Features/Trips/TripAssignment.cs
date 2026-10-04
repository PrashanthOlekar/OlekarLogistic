namespace ProCargo.Application.Features.Trips;

/// <summary>A checked assignment, with the encrypted handover codes, ready to save.</summary>
/// <param name="AcceptedByOwnerId">Set when an owner takes the load, so their acceptance is recorded.</param>
public sealed record TripAssignment(
    long BookingId,
    long VehicleId,
    long DriverId,
    long? AcceptedByOwnerId,
    string PickupOtpProtected,
    string DeliveryOtpProtected,
    long ActorUserId);
