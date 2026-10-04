namespace ProCargo.Domain.Entities;

/// <summary>A one-time sign-in code (dbo.OtpCodes). Only a hash of the code is stored.</summary>
public sealed class OtpCode
{
    public long OtpCodeId { get; set; }

    public string Mobile { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public byte Attempts { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
