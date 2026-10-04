namespace ProCargo.Application.Features.Trips;

/// <summary>A button press in the driver app.</summary>
/// <param name="Action">EnRoute, ReachedPickup, StartTrip or ReachedDestination.</param>
/// <param name="Latitude">The phone's position, if shared.</param>
/// <param name="Longitude">The phone's position, if shared.</param>
public sealed record TripEventRequest(string Action, decimal? Latitude, decimal? Longitude);
