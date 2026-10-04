namespace ProCargo.Domain.Entities;

/// <summary>A customer's request to move goods (dbo.Bookings): the columns the business rules need.</summary>
public sealed class Booking
{
    public long BookingId { get; set; }

    /// <summary>Shown to customers, for example PC-24001.</summary>
    public string BookingNumber { get; set; } = string.Empty;

    public long CustomerId { get; set; }

    public int? PickupCityId { get; set; }

    public int? DropCityId { get; set; }

    public int GoodsCategoryId { get; set; }

    public int VehicleTypeId { get; set; }

    public int WeightKg { get; set; }

    public DateOnly PickupDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
