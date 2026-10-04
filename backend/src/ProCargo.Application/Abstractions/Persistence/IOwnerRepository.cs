using ProCargo.Application.Features.Owners;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IOwnerRepository
{
    Task<Owner?> GetByIdAsync(long ownerId, CancellationToken cancellationToken);

    Task<Owner?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);

    Task<Owner?> GetByMobileAsync(string mobile, CancellationToken cancellationToken);

    /// <summary>Returns false when the owner doesn't exist. Approval also activates the account.</summary>
    Task<bool> SetVerificationAsync(long ownerId, bool approve, string? reason, long adminUserId, CancellationToken cancellationToken);

    Task<OwnerDashboard?> GetDashboardAsync(long ownerId, DateTime monthStartUtc, CancellationToken cancellationToken);
}
