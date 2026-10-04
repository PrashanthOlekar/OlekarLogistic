using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Profiles;

/// <summary>
/// Finds the signed-in user's customer, owner or driver profile,
/// so every service scopes data to "mine" the same way.
/// </summary>
public interface ICurrentProfiles
{
    Task<long> GetCustomerIdAsync(CancellationToken cancellationToken);

    Task<Owner> GetOwnerAsync(CancellationToken cancellationToken);

    Task<Driver> GetDriverAsync(CancellationToken cancellationToken);
}
