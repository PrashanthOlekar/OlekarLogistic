namespace ProCargo.Application.Features.Bookings;

/// <summary>A captured payment to record against a booking's quote.</summary>
public sealed record PaymentRecord(
    long BookingId,
    long QuoteId,
    string Method,
    string Gateway,
    string? GatewayOrderId,
    string? GatewayPaymentId);
