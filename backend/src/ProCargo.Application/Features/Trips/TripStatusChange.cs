namespace ProCargo.Application.Features.Trips;

/// <summary>One step of a trip, checked against the current status inside the database transaction.</summary>
public sealed record TripStatusChange(
    long TripId,
    IReadOnlyList<string> AllowedFrom,
    string NewStatus,
    string EventType,
    string? Note,
    decimal? Latitude,
    decimal? Longitude,
    long ActorUserId);
