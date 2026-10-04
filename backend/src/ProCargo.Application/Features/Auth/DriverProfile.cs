namespace ProCargo.Application.Features.Auth;

public sealed class DriverProfile
{
    public long DriverId { get; set; }

    public string KycStatus { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public string LicenceNumber { get; set; } = string.Empty;

    public string LicenceClass { get; set; } = string.Empty;

    public DateOnly LicenceExpiry { get; set; }

    public string? OwnerName { get; set; }
}
