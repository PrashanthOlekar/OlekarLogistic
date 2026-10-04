using ProCargo.Application.Features.Auth;

namespace ProCargo.Application.Features.Registration;

/// <summary>Creates customer, owner and driver accounts and signs the new user in.</summary>
public interface IRegistrationService
{
    Task<AuthResponse> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> RegisterOwnerAsync(RegisterOwnerRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> RegisterDriverAsync(RegisterDriverRequest request, CancellationToken cancellationToken);
}
