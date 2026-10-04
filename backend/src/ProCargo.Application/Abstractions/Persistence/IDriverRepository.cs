using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Drivers;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(long driverId, CancellationToken cancellationToken);

    Task<Driver?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);

    Task<bool> HasActiveTripAsync(long driverId, CancellationToken cancellationToken);

    /// <param name="ownerId">Only this owner's drivers; null for everyone (admin).</param>
    Task<PagedResult<DriverListItem>> GetPagedAsync(DriverQuery query, long? ownerId, CancellationToken cancellationToken);

    /// <summary>Returns false when the driver doesn't exist. Approval puts the driver on duty and activates the account.</summary>
    Task<bool> SetVerificationAsync(long driverId, bool approve, string? reason, long adminUserId, CancellationToken cancellationToken);
}
