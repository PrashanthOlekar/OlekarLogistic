using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Vehicles;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(long vehicleId, CancellationToken cancellationToken);

    Task<bool> HasActiveTripAsync(long vehicleId, CancellationToken cancellationToken);

    /// <param name="ownerId">Only this owner's vehicles; null for everyone (admin).</param>
    Task<PagedResult<VehicleListItem>> GetPagedAsync(VehicleQuery query, long? ownerId, CancellationToken cancellationToken);

    /// <summary>Saves a new vehicle and returns its id. A duplicate registration number is a conflict.</summary>
    Task<long> CreateAsync(Vehicle vehicle, CancellationToken cancellationToken);

    /// <summary>Changes availability and/or the regular driver. Returns false when the vehicle doesn't exist.</summary>
    Task<bool> PatchAsync(long vehicleId, string? availabilityStatus, bool updateDriver, long? currentDriverId, CancellationToken cancellationToken);

    Task<bool> SetVerificationAsync(long vehicleId, bool approve, CancellationToken cancellationToken);
}
