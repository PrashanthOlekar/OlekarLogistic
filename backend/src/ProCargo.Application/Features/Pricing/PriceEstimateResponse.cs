namespace ProCargo.Application.Features.Pricing;

/// <summary>The price breakdown, and a bigger vehicle when the load is too heavy.</summary>
public sealed record PriceEstimateResponse(
    decimal DistanceKm,
    int Days,
    decimal VehicleCost,
    decimal DriverCost,
    decimal Freight,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal GstPercent,
    bool OverCapacity,
    int MaxLoadKg,
    SuggestedVehicleType? Suggested);
