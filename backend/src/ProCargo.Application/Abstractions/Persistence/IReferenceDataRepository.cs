using ProCargo.Application.Features.ReferenceData;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

/// <summary>Cities, vehicle types and goods categories.</summary>
public interface IReferenceDataRepository
{
    Task<ReferenceDataResponse> GetFormListsAsync(CancellationToken cancellationToken);

    Task<City?> GetCityAsync(int cityId, CancellationToken cancellationToken);

    Task<VehicleType?> GetVehicleTypeAsync(int vehicleTypeId, CancellationToken cancellationToken);

    /// <summary>The smallest active vehicle type that can carry this weight, or null.</summary>
    Task<VehicleType?> GetSmallestVehicleTypeForAsync(int weightKg, CancellationToken cancellationToken);

    Task<GoodsCategory?> GetGoodsCategoryAsync(int goodsCategoryId, CancellationToken cancellationToken);
}
