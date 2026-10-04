using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository(StoredProcedureExecutor database) : IRefreshTokenRepository
{
    public Task CreateAsync(long userId, string tokenHash, string? deviceInfo, DateTime expiresAt, CancellationToken cancellationToken) =>
        database.ExecuteAsync(
            StoredProcedures.RefreshTokenCreate,
            new { UserId = userId, TokenHash = tokenHash, DeviceInfo = Truncate(deviceInfo), ExpiresAt = expiresAt },
            cancellationToken);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<RefreshToken>(StoredProcedures.RefreshTokenGetByHash, new { TokenHash = tokenHash }, cancellationToken);

    public async Task<bool> RotateAsync(string oldTokenHash, string newTokenHash, string? deviceInfo, DateTime expiresAt, CancellationToken cancellationToken) =>
        await database.ExecuteScalarAsync<int>(
            StoredProcedures.RefreshTokenRotate,
            new { OldTokenHash = oldTokenHash, NewTokenHash = newTokenHash, DeviceInfo = Truncate(deviceInfo), ExpiresAt = expiresAt },
            cancellationToken) > 0;

    public Task RevokeAsync(string tokenHash, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.RefreshTokenRevoke, new { TokenHash = tokenHash }, cancellationToken);

    public Task RevokeAllForUserAsync(long userId, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.RefreshTokenRevokeAllForUser, new { UserId = userId }, cancellationToken);

    /// <summary>RefreshTokens.DeviceInfo holds 200 characters.</summary>
    private static string? Truncate(string? deviceInfo) =>
        deviceInfo is { Length: > 200 } ? deviceInfo[..200] : deviceInfo;
}
