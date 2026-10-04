using ProCargo.Domain.Constants;

namespace ProCargo.Domain.Trips;

/// <summary>
/// The driver app moves a trip exactly one step at a time:
///
///   Assigned → EnRouteToPickup → AtPickup ─(pickup code)→ Loaded → InTransit
///            → AtDestination ─(POD photo + delivery code)→ Delivered
/// </summary>
public static class TripSteps
{
    private static readonly IReadOnlyDictionary<string, TripStep> Steps = new Dictionary<string, TripStep>
    {
        [TripAction.EnRoute] = new(
            [TripStatus.Assigned],
            TripStatus.EnRouteToPickup,
            TripEventType.EnRouteToPickup),

        [TripAction.ReachedPickup] = new(
            [TripStatus.Assigned, TripStatus.EnRouteToPickup],
            TripStatus.AtPickup,
            TripEventType.ReachedPickup),

        [TripAction.StartTrip] = new(
            [TripStatus.Loaded],
            TripStatus.InTransit,
            TripEventType.TripStarted),

        [TripAction.ReachedDestination] = new(
            [TripStatus.InTransit],
            TripStatus.AtDestination,
            TripEventType.ReachedDestination),
    };

    public static IReadOnlyCollection<string> Actions => Steps.Keys.ToList();

    public static TripStep? Find(string? action) =>
        action is not null && Steps.TryGetValue(action, out TripStep? step) ? step : null;

    /// <summary>The step that confirms the sender's code: goods are on the truck.</summary>
    public static readonly TripStep PickupHandover = new(
        [TripStatus.AtPickup],
        TripStatus.Loaded,
        TripEventType.PickupOtpVerified);
}
