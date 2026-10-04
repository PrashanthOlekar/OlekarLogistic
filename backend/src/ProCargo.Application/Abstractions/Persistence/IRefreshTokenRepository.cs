using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task CreateAsync(long userId, string tokenHash, string? deviceInfo, DateTime expiresAt, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Revokes the old token and stores the new one in one step. False if the old one was already used or expired.</summary>
    Task<bool> RotateAsync(string oldTokenHash, string newTokenHash, string? deviceInfo, DateTime expiresAt, CancellationToken cancellationToken);

    Task RevokeAsync(string tokenHash, CancellationToken cancellationToken);

    Task RevokeAllForUserAsync(long userId, CancellationToken cancellationToken);
}
