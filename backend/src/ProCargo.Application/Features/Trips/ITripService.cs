using ProCargo.Application.Abstractions.Storage;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Trips;

/// <summary>
/// A trip from assignment to POD approval:
///   assign (owner or admin) → driver steps and handover codes → POD photo → admin approves the POD.
/// </summary>
public interface ITripService
{
    /// <summary>Owners see their trips, drivers theirs, admins all.</summary>
    Task<PagedResult<TripListItem>> GetPagedAsync(TripQuery query, CancellationToken cancellationToken);

    Task<TripDetail> GetAsync(long tripId, CancellationToken cancellationToken);

    Task<CreatedTrip> AssignAsync(AssignTripRequest request, CancellationToken cancellationToken);

    /// <summary>Driver: EnRoute, ReachedPickup, StartTrip or ReachedDestination.</summary>
    Task RecordEventAsync(long tripId, TripEventRequest request, CancellationToken cancellationToken);

    /// <summary>Driver: the sender's or receiver's code.</summary>
    Task ConfirmHandoverAsync(long tripId, HandoverRequest request, CancellationToken cancellationToken);

    /// <summary>Driver: goods photo at pickup, or the signed POD at the destination.</summary>
    Task<CreatedResource> AddPhotoAsync(long tripId, TripPhotoRequest request, FileUpload? file, CancellationToken cancellationToken);

    /// <summary>Admin: the POD is fine. Completes the trip, approves the payout and issues the invoice.</summary>
    Task ApprovePodAsync(long tripId, CancellationToken cancellationToken);
}
