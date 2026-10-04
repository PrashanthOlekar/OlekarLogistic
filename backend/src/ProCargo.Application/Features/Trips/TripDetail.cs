using ProCargo.Application.Features.Bookings;

namespace ProCargo.Application.Features.Trips;

/// <summary>One trip as the driver app shows it.</summary>
/// <param name="HasPod">True once the signed delivery receipt is uploaded.</param>
public sealed record TripDetail(
    long Id,
    string TripNumber,
    string Status,
    string Vehicle,
    bool HasPod,
    decimal? PlannedDistanceKm,
    decimal? DriverPay,
    TripBookingView Booking,
    IReadOnlyList<TripEventItem> Events);
