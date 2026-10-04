namespace ProCargo.Application.Features.Vehicles;

/// <summary>Partial update by the owner. Send only what changes.</summary>
/// <param name="AvailabilityStatus">Available, Busy or Maintenance.</param>
/// <param name="CurrentDriverId">The regular driver for this vehicle.</param>
/// <param name="RemoveDriver">True to remove the regular driver.</param>
public sealed record UpdateVehicleRequest(string? AvailabilityStatus, long? CurrentDriverId, bool RemoveDriver = false);
