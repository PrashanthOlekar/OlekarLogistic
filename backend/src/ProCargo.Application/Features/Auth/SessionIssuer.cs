using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Auth;

internal sealed class SessionIssuer(
    ITokenService tokens,
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    ICurrentUser currentUser) : ISessionIssuer
{
    public async Task<AuthResponse> StartSessionAsync(User user, CancellationToken cancellationToken)
    {
        AccessToken accessToken = tokens.CreateAccessToken(user);
        NewRefreshToken refreshToken = tokens.CreateRefreshToken();

        await refreshTokens.CreateAsync(user.UserId, refreshToken.Hash, currentUser.DeviceInfo, refreshToken.ExpiresAt, cancellationToken);
        await users.RecordLoginAsync(user.UserId, cancellationToken);

        UserProfile profile = await users.GetProfileAsync(user.UserId, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        return new AuthResponse(accessToken.Token, accessToken.ExpiresInSeconds, accessToken.ExpiresAt, refreshToken.Token, profile);
    }
}
