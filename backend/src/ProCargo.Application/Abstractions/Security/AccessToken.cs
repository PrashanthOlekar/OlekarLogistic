namespace ProCargo.Application.Abstractions.Security;

/// <summary>A signed JWT and when it expires.</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAt, int ExpiresInSeconds);
