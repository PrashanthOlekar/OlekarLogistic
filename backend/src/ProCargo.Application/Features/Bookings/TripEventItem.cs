namespace ProCargo.Application.Features.Bookings;

/// <summary>One line of a trip's timeline.</summary>
public sealed class TripEventItem
{
    public string EventType { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}
