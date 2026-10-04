namespace ProCargo.Application.Features.Bookings;

/// <summary>The new booking's id and the number customers see (PC-24001).</summary>
public sealed class CreatedBooking
{
    public long Id { get; set; }

    public string BookingNumber { get; set; } = string.Empty;
}
