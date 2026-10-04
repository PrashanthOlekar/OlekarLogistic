using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Trips;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface ITripRepository
{
    /// <summary>Creates the trip and marks the booking, truck and driver busy, in one transaction.</summary>
    Task<CreatedTrip> AssignAsync(TripAssignment assignment, CancellationToken cancellationToken);

    Task<Trip?> GetByIdAsync(long tripId, CancellationToken cancellationToken);

    /// <param name="ownerId">Only this owner's trips, or null.</param>
    /// <param name="driverId">Only this driver's trips, or null.</param>
    Task<PagedResult<TripListItem>> GetPagedAsync(TripQuery query, long? ownerId, long? driverId, CancellationToken cancellationToken);

    Task<TripDetailData?> GetDetailAsync(long tripId, CancellationToken cancellationToken);

    /// <summary>Moves the trip one step and records the timeline event. Conflict if the status changed meanwhile.</summary>
    Task ChangeStatusAsync(TripStatusChange change, CancellationToken cancellationToken);

    Task<bool> HasPodAsync(long tripId, CancellationToken cancellationToken);

    /// <summary>Delivered: frees the truck and driver and creates the owner's payout.</summary>
    Task CompleteDeliveryAsync(long tripId, long actorUserId, CancellationToken cancellationToken);

    /// <summary>Saves a goods photo or POD and its timeline event. Returns the DocumentId.</summary>
    Task<long> AddPhotoAsync(TripPhoto photo, CancellationToken cancellationToken);

    Task<PodApprovalData?> GetForPodApprovalAsync(long tripId, CancellationToken cancellationToken);

    Task<long> NextInvoiceSequenceAsync(CancellationToken cancellationToken);

    /// <summary>Completes the trip and booking, approves the payout and POD, and issues the invoice.</summary>
    Task ApprovePodAsync(PodApproval approval, CancellationToken cancellationToken);
}
