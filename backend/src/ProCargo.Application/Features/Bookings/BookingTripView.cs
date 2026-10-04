namespace ProCargo.Application.Features.Bookings;

/// <summary>Truck, driver, handover codes and timeline, as the customer sees them.</summary>
/// <param name="PickupOtp">Give this to the driver at loading. Shown to the customer until the goods are loaded.</param>
/// <param name="DeliveryOtp">The receiver gives this to the driver. Shown to the customer until delivery.</param>
public sealed record BookingTripView(
    string TripNumber,
    string Status,
    string Vehicle,
    string DriverName,
    string DriverMobile,
    string? PickupOtp,
    string? DeliveryOtp,
    IReadOnlyList<TripEventItem> Events);
