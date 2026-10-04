using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Features.Loads;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class LoadRepository(StoredProcedureExecutor database) : ILoadRepository
{
    public Task<AvailableLoadsData> GetAvailableAsync(long ownerId, CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(StoredProcedures.LoadGetAvailable, new { OwnerId = ownerId }, async grid =>
        {
            List<LoadRow> loads = (await grid.ReadAsync<LoadRow>()).ToList();
            List<FreeVehicleRow> vehicles = (await grid.ReadAsync<FreeVehicleRow>()).ToList();
            return new AvailableLoadsData(loads, vehicles);
        }, cancellationToken);

    public Task DeclineAsync(long bookingId, long ownerId, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.LoadOfferDecline, new { BookingId = bookingId, OwnerId = ownerId }, cancellationToken);
}
