namespace ProCargo.Application.Features.Settlements;

public sealed class SettlementListItem
{
    public long Id { get; set; }

    /// <summary>Trip number.</summary>
    public string Trip { get; set; } = string.Empty;

    /// <summary>Business name, or the owner's name.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>IFSC and the last 4 digits of the payout account.</summary>
    public string? Bank { get; set; }

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
