namespace ProCargo.Domain.Trips;

/// <summary>One driver action: the statuses it is allowed from, the status it moves to, and the event it records.</summary>
public sealed record TripStep(IReadOnlyList<string> AllowedFrom, string NextStatus, string EventType);
