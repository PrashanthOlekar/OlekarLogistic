namespace ProCargo.Application.Features.Bookings;

/// <summary>The booking part of GET /bookings/{id}, as read from the database.</summary>
public sealed class BookingDetailRow
{
    public long Id { get; set; }

    public string BookingNumber { get; set; } = string.Empty;

    public long CustomerId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateOnly PickupDate { get; set; }

    public string? PickupSlot { get; set; }

    public int WeightKg { get; set; }

    public decimal? GoodsValue { get; set; }

    public string GoodsDescription { get; set; } = string.Empty;

    public string? SpecialInstructions { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CancelledReason { get; set; }

    public string Goods { get; set; } = string.Empty;

    public string VehicleType { get; set; } = string.Empty;

    public string? PickupCity { get; set; }

    public string PickupAddress { get; set; } = string.Empty;

    public string? PickupContactName { get; set; }

    public string? PickupContactPhone { get; set; }

    public string? DropCity { get; set; }

    public string DropAddress { get; set; } = string.Empty;

    public string? DropContactName { get; set; }

    public string? DropContactPhone { get; set; }
}
