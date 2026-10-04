namespace ProCargo.Application.Features.Loads;

/// <summary>A paid booking waiting for a truck, as read from the database.</summary>
public sealed class LoadRow
{
    public long Id { get; set; }

    public string BookingNumber { get; set; } = string.Empty;

    public DateOnly PickupDate { get; set; }

    public string? PickupSlot { get; set; }

    public int WeightKg { get; set; }

    public int VehicleTypeId { get; set; }

    public string Goods { get; set; } = string.Empty;

    public string? From { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string? To { get; set; }

    public string VehicleType { get; set; } = string.Empty;

    public decimal? DistanceKm { get; set; }

    public decimal? Payout { get; set; }
}
