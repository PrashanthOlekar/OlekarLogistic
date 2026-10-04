using ProCargo.Domain.Constants;

namespace ProCargo.Domain.Trips;

/// <summary>
/// The customer sees the pickup code until the goods are loaded, and the delivery code until delivery.
/// </summary>
public static class HandoverCodeVisibility
{
    public static bool ShowPickupCode(string tripStatus) =>
        tripStatus is TripStatus.Assigned or TripStatus.EnRouteToPickup or TripStatus.AtPickup;

    public static bool ShowDeliveryCode(string tripStatus) =>
        tripStatus is not (TripStatus.Delivered or TripStatus.Completed);
}
