namespace ProCargo.Application.Features.Pricing;

/// <summary>An instant price before booking.</summary>
/// <param name="PickupCityId">From GET /reference-data, cities.</param>
/// <param name="DropCityId">From GET /reference-data, cities.</param>
/// <param name="VehicleTypeId">From GET /reference-data, vehicleTypes.</param>
/// <param name="WeightKg">Approximate weight of the goods.</param>
public sealed record PriceEstimateRequest(int PickupCityId, int DropCityId, int VehicleTypeId, int WeightKg);
