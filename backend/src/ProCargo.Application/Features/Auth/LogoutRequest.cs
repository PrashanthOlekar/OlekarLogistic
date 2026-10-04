namespace ProCargo.Application.Features.Auth;

/// <summary>Sign out on this device: the refresh token stops working.</summary>
public sealed record LogoutRequest(string? RefreshToken);
