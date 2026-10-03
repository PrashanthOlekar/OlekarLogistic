namespace ProCargo.Api.Services;

/// <summary>
/// The full price of a trip.
/// The customer pays TotalAmount (freight + GST).
/// The owner receives OwnerPayout (freight − ProCargo commission).
/// </summary>
public record PriceBreakdown(
    decimal DistanceKm,
    int Days,
    decimal VehicleCost,
    decimal DriverCost,
    decimal Freight,
    decimal GstPercent,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal CommissionPercent,
    decimal Commission,
    decimal OwnerPayout);
