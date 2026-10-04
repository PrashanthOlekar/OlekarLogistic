namespace ProCargo.Application.Features.Bookings;

public sealed class BookingListItem
{
    public long Id { get; set; }

    public string BookingNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    /// <summary>Company name, or the customer's name.</summary>
    public string Customer { get; set; } = string.Empty;

    public string CustomerMobile { get; set; } = string.Empty;

    public string? From { get; set; }

    public string? To { get; set; }

    public DateOnly PickupDate { get; set; }

    public int WeightKg { get; set; }

    public string VehicleType { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Total of the latest quote.</summary>
    public decimal? Total { get; set; }
}
