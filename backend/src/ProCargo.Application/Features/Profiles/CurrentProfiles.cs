using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Profiles;

/// <summary>Scoped: each profile is looked up at most once per request.</summary>
internal sealed class CurrentProfiles(
    ICurrentUser currentUser,
    ICustomerRepository customers,
    IOwnerRepository owners,
    IDriverRepository drivers) : ICurrentProfiles
{
    private long? _customerId;
    private Owner? _owner;
    private Driver? _driver;

    public async Task<long> GetCustomerIdAsync(CancellationToken cancellationToken)
    {
        if (_customerId is null)
        {
            Customer customer = await customers.GetByUserIdAsync(currentUser.UserId, cancellationToken)
                ?? throw new ForbiddenException("Customer profile not found.");
            _customerId = customer.CustomerId;
        }

        return _customerId.Value;
    }

    public async Task<Owner> GetOwnerAsync(CancellationToken cancellationToken)
    {
        _owner ??= await owners.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new ForbiddenException("Owner profile not found.");
        return _owner;
    }

    public async Task<Driver> GetDriverAsync(CancellationToken cancellationToken)
    {
        _driver ??= await drivers.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new ForbiddenException("Driver profile not found.");
        return _driver;
    }
}
