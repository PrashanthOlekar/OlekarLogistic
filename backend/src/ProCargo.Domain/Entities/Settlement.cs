namespace ProCargo.Domain.Entities;

/// <summary>An owner's payout for one trip (dbo.Settlements).</summary>
public sealed class Settlement
{
    public long SettlementId { get; set; }

    public long TripId { get; set; }

    public long OwnerId { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal TdsAmount { get; set; }

    public decimal NetAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>The bank's transfer reference.</summary>
    public string? Utr { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
