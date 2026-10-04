using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Vehicles;

public interface IVehicleService
{
    /// <summary>Owners see their own vehicles; admins see everyone's.</summary>
    Task<PagedResult<VehicleListItem>> GetPagedAsync(VehicleQuery query, CancellationToken cancellationToken);

    Task<CreatedResource> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken);

    Task UpdateAsync(long vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken);

    /// <summary>Admin: approve or reject a vehicle.</summary>
    Task SetVerificationAsync(long vehicleId, VerificationRequest request, CancellationToken cancellationToken);
}
