namespace ProCargo.Application.Abstractions.Security;

/// <summary>A new refresh token: the value for the client, the hash for the database, and its expiry.</summary>
public sealed record NewRefreshToken(string Token, string Hash, DateTime ExpiresAt);
