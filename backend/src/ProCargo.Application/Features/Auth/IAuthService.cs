namespace ProCargo.Application.Features.Auth;

/// <summary>Sign-in with mobile number and one-time code, token refresh, sign-out and the profile.</summary>
public interface IAuthService
{
    Task<SendOtpResponse> SendCodeAsync(SendOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);

    Task<UserProfile> GetProfileAsync(CancellationToken cancellationToken);
}
