namespace ProCargo.Domain.Entities;

/// <summary>Keeps a user signed in on a device (dbo.RefreshTokens). Only a hash of the token is stored.</summary>
public sealed class RefreshToken
{
    public long RefreshTokenId { get; set; }

    public long UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
