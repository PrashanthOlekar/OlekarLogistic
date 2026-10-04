namespace ProCargo.Application.Features.Bookings;

/// <summary>A verified, free truck that fits a booking, with its owner's free verified drivers.</summary>
public sealed class AssignableVehicle
{
    public long Id { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public long? CurrentDriverId { get; set; }

    public List<AssignableDriver> Drivers { get; set; } = [];
}
