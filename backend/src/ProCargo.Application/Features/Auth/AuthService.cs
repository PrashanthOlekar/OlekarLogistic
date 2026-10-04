using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Messaging;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Auth;

internal sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IOtpService otp,
    ISmsSender sms,
    ISessionIssuer sessions,
    ITokenService tokens,
    ICurrentUser currentUser,
    IOptions<OtpOptions> otpOptions,
    TimeProvider clock,
    IValidator<SendOtpRequest> sendOtpValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshValidator,
    ILogger<AuthService> logger) : IAuthService
{
    private const string SessionEndedMessage = "Your session has ended. Please sign in again.";

    public async Task<SendOtpResponse> SendCodeAsync(SendOtpRequest request, CancellationToken cancellationToken)
    {
        await sendOtpValidator.ValidateAndThrowAsync(request, cancellationToken);
        string mobile = MobileNumber.TryNormalize(request.Mobile)!;

        bool accountExists = await users.GetByMobileAsync(mobile, cancellationToken) is not null;
        if (request.Purpose == OtpPurpose.Login && !accountExists)
        {
            throw new NotFoundException("No account uses this number yet. Register first.");
        }
        if (request.Purpose == OtpPurpose.Signup && accountExists)
        {
            throw new ConflictException("This number is already registered. Sign in instead.");
        }

        string code = await otp.IssueAsync(mobile, request.Purpose, cancellationToken);
        await sms.SendOtpAsync(mobile, code, cancellationToken);

        logger.LogInformation("Sign-in code sent to mobile ending {MobileEnd} for {Purpose}", mobile[^4..], request.Purpose);

        return new SendOtpResponse(true, otpOptions.Value.ShowCodeInResponse ? code : null);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        string mobile = MobileNumber.TryNormalize(request.Mobile)!;

        try
        {
            await otp.VerifyAsync(mobile, request.Code, OtpPurpose.Login, cancellationToken);
        }
        catch (BusinessRuleException)
        {
            logger.LogWarning("Failed sign-in for mobile ending {MobileEnd}", mobile[^4..]);
            throw;
        }

        User user = await users.GetByMobileAsync(mobile, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        if (UserStatus.IsLockedOut(user.Status))
        {
            logger.LogWarning("Blocked account {UserId} tried to sign in", user.UserId);
            throw new ForbiddenException("This account is not active. Contact ProCargo support.");
        }

        logger.LogInformation("User {UserId} ({Role}) signed in", user.UserId, user.Role);
        return await sessions.StartSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await refreshValidator.ValidateAndThrowAsync(request, cancellationToken);
        string oldHash = tokens.HashRefreshToken(request.RefreshToken);

        var stored = await refreshTokens.GetByHashAsync(oldHash, cancellationToken)
            ?? throw new UnauthorizedAccessException(SessionEndedMessage);

        if (stored.RevokedAt is not null)
        {
            // An already-used token came back: it may have been stolen, so end every session of this user.
            logger.LogWarning("Re-used refresh token for user {UserId}; revoking all sessions", stored.UserId);
            await refreshTokens.RevokeAllForUserAsync(stored.UserId, cancellationToken);
            throw new UnauthorizedAccessException(SessionEndedMessage);
        }

        if (stored.ExpiresAt <= clock.GetUtcNow().UtcDateTime)
        {
            throw new UnauthorizedAccessException(SessionEndedMessage);
        }

        User user = await users.GetByIdAsync(stored.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException(SessionEndedMessage);

        if (UserStatus.IsLockedOut(user.Status))
        {
            await refreshTokens.RevokeAllForUserAsync(user.UserId, cancellationToken);
            throw new ForbiddenException("This account is not active. Contact ProCargo support.");
        }

        NewRefreshToken next = tokens.CreateRefreshToken();
        bool rotated = await refreshTokens.RotateAsync(oldHash, next.Hash, currentUser.DeviceInfo, next.ExpiresAt, cancellationToken);
        if (!rotated)
        {
            throw new UnauthorizedAccessException(SessionEndedMessage);
        }

        AccessToken accessToken = tokens.CreateAccessToken(user);
        UserProfile profile = await users.GetProfileAsync(user.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException(SessionEndedMessage);

        return new AuthResponse(accessToken.Token, accessToken.ExpiresInSeconds, accessToken.ExpiresAt, next.Token, profile);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await refreshTokens.RevokeAsync(tokens.HashRefreshToken(request.RefreshToken), cancellationToken);
        }

        if (currentUser.IsAuthenticated)
        {
            logger.LogInformation("User {UserId} signed out", currentUser.UserId);
        }
    }

    public async Task<UserProfile> GetProfileAsync(CancellationToken cancellationToken)
    {
        return await users.GetProfileAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Account not found.");
    }
}
