namespace ProCargo.Application.Features.Bookings;

/// <summary>The active trip of a booking, as read from the database (codes still encrypted).</summary>
public sealed class BookingTripRow
{
    public long TripId { get; set; }

    public string TripNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Vehicle { get; set; } = string.Empty;

    public string DriverName { get; set; } = string.Empty;

    public string DriverMobile { get; set; } = string.Empty;

    public string? PickupOtpProtected { get; set; }

    public string? DeliveryOtpProtected { get; set; }
}
