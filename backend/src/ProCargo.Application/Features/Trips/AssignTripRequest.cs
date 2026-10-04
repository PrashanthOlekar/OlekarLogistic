namespace ProCargo.Application.Features.Trips;

/// <summary>
/// Put a truck and driver on a paid booking. Owners use it to take a load with their own truck;
/// admins use it to assign any verified truck.
/// </summary>
public sealed record AssignTripRequest(long BookingId, long VehicleId, long DriverId);
