using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Features.Approvals;
using ProCargo.Application.Features.Common;
using ProCargo.Domain.Constants;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class ApprovalRepository(StoredProcedureExecutor database) : IApprovalRepository
{
    public Task<PendingApprovals> GetPendingAsync(CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(StoredProcedures.ApprovalGetPending, null, async grid =>
        {
            List<PendingOwner> owners = (await grid.ReadAsync<PendingOwner>()).ToList();
            List<PendingDriver> drivers = (await grid.ReadAsync<PendingDriver>()).ToList();
            List<PendingVehicle> vehicles = (await grid.ReadAsync<PendingVehicle>()).ToList();
            ILookup<(string EntityType, long EntityId), DocumentChip> documents = (await grid.ReadAsync<ApplicantDocumentRow>())
                .ToLookup(row => (row.EntityType, row.EntityId), row => row.ToChip());

            owners.ForEach(owner => owner.Documents = documents[(DocumentEntity.Owner, owner.Id)].ToList());
            drivers.ForEach(driver => driver.Documents = documents[(DocumentEntity.Driver, driver.Id)].ToList());
            vehicles.ForEach(vehicle => vehicle.Documents = documents[(DocumentEntity.Vehicle, vehicle.Id)].ToList());

            return new PendingApprovals { Owners = owners, Drivers = drivers, Vehicles = vehicles };
        }, cancellationToken);

    private sealed class ApplicantDocumentRow
    {
        public string EntityType { get; set; } = string.Empty;

        public long EntityId { get; set; }

        public long Id { get; set; }

        public string DocType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateOnly? ExpiryDate { get; set; }

        public DocumentChip ToChip() => new() { Id = Id, DocType = DocType, Status = Status, ExpiryDate = ExpiryDate };
    }
}
