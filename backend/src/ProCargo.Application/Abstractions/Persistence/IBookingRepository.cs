using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Bookings;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IBookingRepository
{
    /// <summary>Saves the booking and its first quote together.</summary>
    Task<CreatedBooking> CreateAsync(NewBooking booking, Quote quote, CancellationToken cancellationToken);

    Task<Booking?> GetByIdAsync(long bookingId, CancellationToken cancellationToken);

    /// <param name="customerId">Only this customer's bookings; null for everyone (admin).</param>
    Task<PagedResult<BookingListItem>> GetPagedAsync(BookingQuery query, long? customerId, CancellationToken cancellationToken);

    Task<BookingDetailData?> GetDetailAsync(long bookingId, CancellationToken cancellationToken);

    /// <summary>The newest quote still waiting for payment (it may have expired).</summary>
    Task<Quote?> GetLatestOpenQuoteAsync(long bookingId, CancellationToken cancellationToken);

    /// <summary>Replaces the open quotes with a new one. Returns the new QuoteId.</summary>
    Task<long> RequoteAsync(long bookingId, Quote quote, CancellationToken cancellationToken);

    /// <summary>Records a captured payment: quote Accepted, booking Confirmed.</summary>
    Task<PaymentReceipt> PayAsync(PaymentRecord payment, CancellationToken cancellationToken);

    /// <summary>Cancels before loading: frees the truck and driver, refunds captured payments.</summary>
    Task CancelAsync(long bookingId, long cancelledBy, string? reason, CancellationToken cancellationToken);

    Task<IReadOnlyList<AssignableVehicle>> GetAssignableVehiclesAsync(long bookingId, CancellationToken cancellationToken);
}
