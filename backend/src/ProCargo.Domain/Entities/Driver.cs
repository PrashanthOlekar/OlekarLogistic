namespace ProCargo.Domain.Entities;

/// <summary>A driver's profile (dbo.Drivers). OwnerId links them to the owner whose trucks they drive.</summary>
public sealed class Driver
{
    public long DriverId { get; set; }

    public long UserId { get; set; }

    public long? OwnerId { get; set; }

    public string LicenceNumber { get; set; } = string.Empty;

    public string LicenceClass { get; set; } = string.Empty;

    public DateOnly LicenceExpiry { get; set; }

    public string KycStatus { get; set; } = string.Empty;

    public string DutyStatus { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public decimal? Rating { get; set; }
}
