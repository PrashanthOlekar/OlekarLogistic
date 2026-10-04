namespace ProCargo.Application.Features.Auth;

/// <summary>What the portal receives after signing in, registering or refreshing.</summary>
/// <param name="AccessToken">Send as "Authorization: Bearer ..." on every request.</param>
/// <param name="ExpiresIn">Seconds until the access token expires.</param>
/// <param name="ExpiresAt">When the access token expires (UTC).</param>
/// <param name="RefreshToken">Use once with POST /auth/refresh to get new tokens.</param>
/// <param name="User">The signed-in user.</param>
public sealed record AuthResponse(string AccessToken, int ExpiresIn, DateTime ExpiresAt, string RefreshToken, UserProfile User);
