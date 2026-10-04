namespace ProCargo.Domain.Trips;

/// <summary>The buttons in the driver app that move a trip one step (codes and photos are separate).</summary>
public static class TripAction
{
    public const string EnRoute = "EnRoute";
    public const string ReachedPickup = "ReachedPickup";
    public const string StartTrip = "StartTrip";
    public const string ReachedDestination = "ReachedDestination";
}
