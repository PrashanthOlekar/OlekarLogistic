using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Features.ReferenceData;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class ReferenceDataRepository(StoredProcedureExecutor database) : IReferenceDataRepository
{
    public Task<ReferenceDataResponse> GetFormListsAsync(CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(StoredProcedures.ReferenceDataGet, null, async grid => new ReferenceDataResponse
        {
            Cities = (await grid.ReadAsync<CityOption>()).ToList(),
            VehicleTypes = (await grid.ReadAsync<VehicleTypeOption>()).ToList(),
            Goods = (await grid.ReadAsync<GoodsOption>()).ToList(),
        }, cancellationToken);

    public Task<City?> GetCityAsync(int cityId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<City>(StoredProcedures.CityGetById, new { CityId = cityId }, cancellationToken);

    public Task<VehicleType?> GetVehicleTypeAsync(int vehicleTypeId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<VehicleType>(StoredProcedures.VehicleTypeGetById, new { VehicleTypeId = vehicleTypeId }, cancellationToken);

    public Task<VehicleType?> GetSmallestVehicleTypeForAsync(int weightKg, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<VehicleType>(StoredProcedures.VehicleTypeGetSmallestForWeight, new { WeightKg = weightKg }, cancellationToken);

    public Task<GoodsCategory?> GetGoodsCategoryAsync(int goodsCategoryId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<GoodsCategory>(StoredProcedures.GoodsCategoryGetById, new { GoodsCategoryId = goodsCategoryId }, cancellationToken);
}
