using ProCargo.Application.Features.Registration;

namespace ProCargo.Application.Abstractions.Persistence;

/// <summary>Creates an account and its profile in one transaction. Each method returns the new UserId.</summary>
public interface IRegistrationRepository
{
    Task<long> CreateCustomerAsync(NewCustomerAccount account, CancellationToken cancellationToken);

    Task<long> CreateOwnerAsync(NewOwnerAccount account, CancellationToken cancellationToken);

    Task<long> CreateDriverAsync(NewDriverAccount account, CancellationToken cancellationToken);

    Task<bool> LicenceExistsAsync(string licenceNumber, CancellationToken cancellationToken);
}
