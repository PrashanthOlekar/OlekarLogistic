using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// Profile and licence of a driver, linked to the owner whose trucks they drive. Table: Drivers.
/// </summary>
public class Driver
{
    public long DriverId { get; set; }

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public long? OwnerId { get; set; }

    public Owner? Owner { get; set; }

    public string LicenceNumber { get; set; } = "";

    // LMV | TRANSPORT | HGMV | HPMV
    public string LicenceClass { get; set; } = "LMV";

    public DateOnly LicenceExpiry { get; set; }

    public string? AadhaarLast4 { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string KycStatus { get; set; } = "Pending";

    // Available | OnTrip | OffDuty
    public string DutyStatus { get; set; } = "OffDuty";

    public string? RejectionReason { get; set; }

    public long? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [Precision(3, 2)]
    public decimal? Rating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
