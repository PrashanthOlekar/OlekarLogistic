namespace ProCargo.Application.Features.Trips;

/// <summary>One trip with its booking and stops, as read from the database.</summary>
public sealed class TripDetailRow
{
    public long Id { get; set; }

    public string TripNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public long OwnerId { get; set; }

    public long DriverId { get; set; }

    public string Vehicle { get; set; } = string.Empty;

    public bool HasPod { get; set; }

    public decimal? PlannedDistanceKm { get; set; }

    public decimal? DriverPay { get; set; }

    public string BookingNumber { get; set; } = string.Empty;

    public string GoodsDescription { get; set; } = string.Empty;

    public int WeightKg { get; set; }

    public DateOnly PickupDate { get; set; }

    public string? PickupSlot { get; set; }

    public string? SpecialInstructions { get; set; }

    public string? PickupCity { get; set; }

    public string PickupAddress { get; set; } = string.Empty;

    public string? PickupContactName { get; set; }

    public string? PickupContactPhone { get; set; }

    public string? DropCity { get; set; }

    public string DropAddress { get; set; } = string.Empty;

    public string? DropContactName { get; set; }

    public string? DropContactPhone { get; set; }
}
