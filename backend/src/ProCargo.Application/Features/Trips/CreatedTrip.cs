namespace ProCargo.Application.Features.Trips;

public sealed class CreatedTrip
{
    public long Id { get; set; }

    public string TripNumber { get; set; } = string.Empty;
}
