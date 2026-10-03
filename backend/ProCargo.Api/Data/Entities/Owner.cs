using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// Profile and KYC of a lorry owner. Table: Owners.
/// </summary>
public class Owner
{
    public long OwnerId { get; set; }

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public string? BusinessName { get; set; }

    public byte[]? PanEncrypted { get; set; }

    public string? PanLast4 { get; set; }

    public string? AadhaarLast4 { get; set; }

    public string? AadhaarVaultRef { get; set; }

    public string? GSTIN { get; set; }

    // Pending | Approved | Rejected
    public string KycStatus { get; set; } = "Pending";

    public string? RejectionReason { get; set; }

    public long? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [Precision(3, 2)]
    public decimal? Rating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<Vehicle> Vehicles { get; set; } = new();

    public List<Driver> Drivers { get; set; } = new();
}
