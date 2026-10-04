namespace ProCargo.Application.Features.Bookings;

/// <summary>The latest price on a booking.</summary>
/// <param name="Expired">True when the quote is still unpaid and past ValidUntil.</param>
public sealed record BookingQuoteView(
    decimal DistanceKm,
    decimal VehicleCost,
    decimal DriverCost,
    decimal LoadingCharges,
    decimal TaxAmount,
    decimal TotalAmount,
    DateTime ValidUntil,
    string Status,
    bool Expired);
