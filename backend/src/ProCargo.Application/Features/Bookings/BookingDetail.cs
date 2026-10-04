namespace ProCargo.Application.Features.Bookings;

/// <summary>One booking with its price, payment, truck, codes and invoice.</summary>
public sealed record BookingDetail(
    long Id,
    string BookingNumber,
    string Status,
    DateOnly PickupDate,
    string? PickupSlot,
    int WeightKg,
    decimal? GoodsValue,
    string GoodsDescription,
    string? SpecialInstructions,
    DateTime CreatedAt,
    string? CancelledReason,
    string Goods,
    string VehicleType,
    Stop Pickup,
    Stop Drop,
    BookingQuoteView? Quote,
    PaymentSummary? Payment,
    BookingTripView? Trip,
    InvoiceSummary? Invoice);
