using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Vehicles;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class VehicleRepository(StoredProcedureExecutor database) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(long vehicleId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Vehicle>(StoredProcedures.VehicleGetById, new { VehicleId = vehicleId }, cancellationToken);

    public Task<bool> HasActiveTripAsync(long vehicleId, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<bool>(StoredProcedures.VehicleHasActiveTrip, new { VehicleId = vehicleId }, cancellationToken);

    /// <summary>The procedure returns the page, the total, then the documents of the vehicles on the page.</summary>
    public Task<PagedResult<VehicleListItem>> GetPagedAsync(VehicleQuery query, long? ownerId, CancellationToken cancellationToken)
    {
        PageRequest page = query.ToPage();

        var parameters = new DynamicParameters();
        parameters.Add("OwnerId", ownerId, DbType.Int64);
        parameters.Add("VehicleTypeId", query.VehicleTypeId, DbType.Int32);
        parameters.Add("AvailabilityStatus", query.AvailabilityStatus, DbType.String);
        parameters.Add("VerificationStatus", query.VerificationStatus, DbType.String);
        parameters.Add("Search", query.CleanSearch, DbType.String);
        parameters.Add("Sort", query.Sort ?? "Registration", DbType.String);
        parameters.Add("PageNumber", page.PageNumber, DbType.Int32);
        parameters.Add("PageSize", page.PageSize, DbType.Int32);

        return database.QueryMultipleAsync(StoredProcedures.VehicleGetPaged, parameters, async grid =>
        {
            List<VehicleListItem> vehicles = (await grid.ReadAsync<VehicleListItem>()).AsList();
            long total = await grid.ReadSingleAsync<long>();
            IEnumerable<VehicleDocumentRow> documents = await grid.ReadAsync<VehicleDocumentRow>();

            ILookup<long, VehicleDocumentRow> byVehicle = documents.ToLookup(document => document.VehicleId);
            foreach (VehicleListItem vehicle in vehicles)
            {
                vehicle.Documents = byVehicle[vehicle.Id].Select(row => row.ToChip()).ToList();
            }

            return PagedResult<VehicleListItem>.Create(vehicles, page, total);
        }, cancellationToken);
    }

    public Task<long> CreateAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<long>(StoredProcedures.VehicleCreate, new
        {
            vehicle.OwnerId,
            vehicle.VehicleTypeId,
            vehicle.RegistrationNumber,
            vehicle.CapacityKg,
            vehicle.MakeModel,
            vehicle.ManufactureYear,
            vehicle.HomeCityId,
        }, cancellationToken);

    public async Task<bool> PatchAsync(long vehicleId, string? availabilityStatus, bool updateDriver, long? currentDriverId, CancellationToken cancellationToken) =>
        await database.ExecuteScalarAsync<int>(
            StoredProcedures.VehiclePatch,
            new { VehicleId = vehicleId, AvailabilityStatus = availabilityStatus, UpdateDriver = updateDriver, CurrentDriverId = currentDriverId },
            cancellationToken) > 0;

    public async Task<bool> SetVerificationAsync(long vehicleId, bool approve, CancellationToken cancellationToken) =>
        await database.ExecuteScalarAsync<int>(
            StoredProcedures.VehicleSetVerification,
            new { VehicleId = vehicleId, Approve = approve },
            cancellationToken) > 0;

    private sealed class VehicleDocumentRow
    {
        public long VehicleId { get; set; }

        public long Id { get; set; }

        public string DocType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateOnly? ExpiryDate { get; set; }

        public DocumentChip ToChip() => new() { Id = Id, DocType = DocType, Status = Status, ExpiryDate = ExpiryDate };
    }
}
