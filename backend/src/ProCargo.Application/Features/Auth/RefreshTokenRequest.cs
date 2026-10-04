namespace ProCargo.Application.Features.Auth;

/// <summary>Swap a refresh token for a new access token (and a new refresh token).</summary>
public sealed record RefreshTokenRequest(string RefreshToken);
