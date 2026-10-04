using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Bookings;

/// <summary>Filters for GET /bookings.</summary>
public sealed class BookingQuery : ListQuery
{
    /// <summary>Quoted, Confirmed, Assigned, InTransit, Delivered, Completed or Cancelled.</summary>
    public string? Status { get; set; }

    /// <summary>Newest (default) or PickupDate.</summary>
    public string? Sort { get; set; }
}
