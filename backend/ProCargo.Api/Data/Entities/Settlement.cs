using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A payout to a lorry owner after delivery. Table: Settlements.
/// </summary>
public class Settlement
{
    public long SettlementId { get; set; }

    public long TripId { get; set; }

    public Trip Trip { get; set; } = null!;

    public long OwnerId { get; set; }

    public Owner Owner { get; set; } = null!;

    public long? OwnerBankAccountId { get; set; }

    [Precision(12, 2)]
    public decimal GrossAmount { get; set; }

    [Precision(12, 2)]
    public decimal CommissionAmount { get; set; }

    [Precision(12, 2)]
    public decimal TdsAmount { get; set; }

    [Precision(12, 2)]
    public decimal NetAmount { get; set; }

    // AwaitingPod | Approved | Processing | Released | Failed | OnHold
    public string Status { get; set; } = "AwaitingPod";

    public string? PayoutReference { get; set; }

    public string? UTR { get; set; }

    public long? ApprovedBy { get; set; }

    public long? ReleasedBy { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
