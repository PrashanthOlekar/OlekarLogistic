namespace ProCargo.Domain.Entities;

/// <summary>A lorry owner's profile and KYC state (dbo.Owners). PAN itself is stored encrypted and never read back.</summary>
public sealed class Owner
{
    public long OwnerId { get; set; }

    public long UserId { get; set; }

    public string? BusinessName { get; set; }

    public string? PanLast4 { get; set; }

    public string? AadhaarLast4 { get; set; }

    public string KycStatus { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
}
