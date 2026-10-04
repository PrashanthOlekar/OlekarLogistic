using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Bookings;

/// <summary>Everything the database returns for one booking page.</summary>
public sealed class BookingDetailData
{
    public required BookingDetailRow Booking { get; init; }

    public Quote? LatestQuote { get; init; }

    public PaymentSummary? Payment { get; init; }

    public InvoiceSummary? Invoice { get; init; }

    public BookingTripRow? Trip { get; init; }

    public IReadOnlyList<TripEventItem> Events { get; init; } = [];
}
