namespace ProCargo.Application.Features.Trips;

/// <summary>The form fields sent with a trip photo.</summary>
/// <param name="Kind">PickupPhoto (goods at loading) or POD (signed delivery receipt).</param>
public sealed record TripPhotoRequest(string Kind, decimal? Latitude, decimal? Longitude);
