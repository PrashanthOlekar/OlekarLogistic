using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Auth;

/// <summary>Creates the access and refresh tokens for a user who just proved who they are.</summary>
public interface ISessionIssuer
{
    Task<AuthResponse> StartSessionAsync(User user, CancellationToken cancellationToken);
}
