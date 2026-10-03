namespace ProCargo.Api.Contracts;

// What the driver app sends to the /api/driver endpoints.

/// <summary>Action: EnRoute, ReachedPickup, StartTrip or ReachedDestination.</summary>
public record AdvanceTripRequest(string Action, decimal? Latitude, decimal? Longitude);

/// <summary>Kind: Pickup or Delivery. Code: the 4-digit code from the sender or receiver.</summary>
public record VerifyTripCodeRequest(string Kind, string Code);
