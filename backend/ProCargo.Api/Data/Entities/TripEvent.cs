using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// One step in a trip timeline, shown to the customer. Table: TripEvents.
/// </summary>
public class TripEvent
{
    public long TripEventId { get; set; }

    public long TripId { get; set; }

    public string EventType { get; set; } = "";

    public string? Note { get; set; }

    [Precision(9, 6)]
    public decimal? Latitude { get; set; }

    [Precision(9, 6)]
    public decimal? Longitude { get; set; }

    public long? DocumentId { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
