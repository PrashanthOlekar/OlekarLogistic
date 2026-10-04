namespace ProCargo.Application.Features.Loads;

/// <summary>A paid booking with the owner's trucks that can carry it.</summary>
/// <param name="Payout">What the owner receives for this trip.</param>
public sealed record AvailableLoad(
    long Id,
    string BookingNumber,
    DateOnly PickupDate,
    string? PickupSlot,
    int WeightKg,
    string Goods,
    string? From,
    string FromAddress,
    string? To,
    string VehicleType,
    decimal? DistanceKm,
    decimal? Payout,
    IReadOnlyList<LoadVehicle> Vehicles);
