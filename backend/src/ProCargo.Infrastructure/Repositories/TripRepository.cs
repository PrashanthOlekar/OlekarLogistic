using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Bookings;
using ProCargo.Application.Features.Trips;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class TripRepository(StoredProcedureExecutor database) : ITripRepository
{
    public async Task<CreatedTrip> AssignAsync(TripAssignment assignment, CancellationToken cancellationToken) =>
        await database.QuerySingleOrDefaultAsync<CreatedTrip>(StoredProcedures.TripAssign, new
        {
            assignment.BookingId,
            assignment.VehicleId,
            assignment.DriverId,
            assignment.AcceptedByOwnerId,
            assignment.PickupOtpProtected,
            assignment.DeliveryOtpProtected,
            assignment.ActorUserId,
        }, cancellationToken)
        ?? throw new InvalidOperationException("usp_Trip_Assign returned no row.");

    public Task<Trip?> GetByIdAsync(long tripId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Trip>(StoredProcedures.TripGetById, new { TripId = tripId }, cancellationToken);

    public Task<PagedResult<TripListItem>> GetPagedAsync(TripQuery query, long? ownerId, long? driverId, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("OwnerId", ownerId, DbType.Int64);
        parameters.Add("DriverId", driverId, DbType.Int64);
        parameters.Add("Status", query.Status, DbType.String);
        parameters.Add("Search", query.CleanSearch, DbType.String);
        parameters.Add("Sort", query.Sort ?? "Newest", DbType.String);

        return database.QueryPagedAsync<TripListItem>(StoredProcedures.TripGetPaged, parameters, query.ToPage(), cancellationToken);
    }

    public Task<TripDetailData?> GetDetailAsync(long tripId, CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(StoredProcedures.TripGetDetail, new { TripId = tripId }, async grid =>
        {
            TripDetailRow? trip = await grid.ReadFirstOrDefaultAsync<TripDetailRow>();
            List<TripEventItem> events = (await grid.ReadAsync<TripEventItem>()).AsList();
            return trip is null ? null : new TripDetailData(trip, events);
        }, cancellationToken);

    public Task ChangeStatusAsync(TripStatusChange change, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.TripChangeStatus, new
        {
            change.TripId,
            AllowedFrom = string.Join(',', change.AllowedFrom),
            change.NewStatus,
            change.EventType,
            change.Note,
            change.Latitude,
            change.Longitude,
            change.ActorUserId,
        }, cancellationToken);

    public Task<bool> HasPodAsync(long tripId, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<bool>(StoredProcedures.TripHasPod, new { TripId = tripId }, cancellationToken);

    public Task CompleteDeliveryAsync(long tripId, long actorUserId, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.TripCompleteDelivery, new { TripId = tripId, ActorUserId = actorUserId }, cancellationToken);

    public Task<long> AddPhotoAsync(TripPhoto photo, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<long>(StoredProcedures.TripAddPhoto, new
        {
            photo.TripId,
            photo.DocType,
            photo.BlobPath,
            photo.FileName,
            photo.ContentType,
            photo.SizeBytes,
            photo.Sha256,
            photo.Latitude,
            photo.Longitude,
            photo.UploadedBy,
        }, cancellationToken);

    public Task<PodApprovalData?> GetForPodApprovalAsync(long tripId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<PodApprovalData>(StoredProcedures.TripGetForPodApproval, new { TripId = tripId }, cancellationToken);

    public Task<long> NextInvoiceSequenceAsync(CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<long>(StoredProcedures.InvoiceNextSequence, null, cancellationToken);

    public Task ApprovePodAsync(PodApproval approval, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.TripApprovePod, new
        {
            approval.TripId,
            approval.AdminUserId,
            approval.InvoiceNumber,
            approval.PlaceOfSupply,
            approval.CustomerGstin,
            approval.TaxableAmount,
            approval.Cgst,
            approval.Sgst,
            approval.Igst,
            approval.TotalAmount,
        }, cancellationToken);
}
