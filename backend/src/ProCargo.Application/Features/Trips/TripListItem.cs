namespace ProCargo.Application.Features.Trips;

public sealed class TripListItem
{
    public long Id { get; set; }

    public string TripNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string BookingNumber { get; set; } = string.Empty;

    public string? From { get; set; }

    public string? To { get; set; }

    public DateOnly PickupDate { get; set; }

    public string Vehicle { get; set; } = string.Empty;

    public string Driver { get; set; } = string.Empty;

    public string DriverMobile { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    /// <summary>What the owner receives. Not shown to drivers.</summary>
    public decimal? OwnerPayout { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    /// <summary>DocumentId of the latest signed delivery receipt, if uploaded.</summary>
    public long? Pod { get; set; }

    public string? LastEvent { get; set; }
}
