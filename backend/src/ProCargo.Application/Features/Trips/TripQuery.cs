using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Trips;

/// <summary>Filters for GET /trips.</summary>
public sealed class TripQuery : ListQuery
{
    /// <summary>Assigned, EnRouteToPickup, AtPickup, Loaded, InTransit, AtDestination, Delivered, Completed or Cancelled.</summary>
    public string? Status { get; set; }

    /// <summary>Newest (default) or ActiveFirst.</summary>
    public string? Sort { get; set; }
}
