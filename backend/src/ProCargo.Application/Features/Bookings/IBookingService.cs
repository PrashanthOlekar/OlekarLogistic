using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Bookings;

/// <summary>Customers book and pay; admins see every booking and find trucks for them.</summary>
public interface IBookingService
{
    /// <summary>Customers see their own bookings; admins see everyone's.</summary>
    Task<PagedResult<BookingListItem>> GetPagedAsync(BookingQuery query, CancellationToken cancellationToken);

    Task<BookingDetail> GetAsync(long bookingId, CancellationToken cancellationToken);

    Task<CreatedBooking> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken);

    /// <summary>A fresh price after the old quote expired.</summary>
    Task<CreatedResource> RequoteAsync(long bookingId, CancellationToken cancellationToken);

    /// <summary>Pays the open quote (test mode until a payment gateway is connected).</summary>
    Task<PaymentReceipt> PayAsync(long bookingId, PayBookingRequest request, CancellationToken cancellationToken);

    Task CancelAsync(long bookingId, CancelBookingRequest request, CancellationToken cancellationToken);

    /// <summary>Admin: trucks and drivers that could take this booking.</summary>
    Task<IReadOnlyList<AssignableVehicle>> GetAssignableVehiclesAsync(long bookingId, CancellationToken cancellationToken);
}
