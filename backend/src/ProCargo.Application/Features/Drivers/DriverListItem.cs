namespace ProCargo.Application.Features.Drivers;

public sealed class DriverListItem
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string LicenceNumber { get; set; } = string.Empty;

    public string LicenceClass { get; set; } = string.Empty;

    public DateOnly LicenceExpiry { get; set; }

    public string KycStatus { get; set; } = string.Empty;

    public string DutyStatus { get; set; } = string.Empty;

    public decimal? Rating { get; set; }

    /// <summary>The owner the driver works for.</summary>
    public string? OwnerName { get; set; }

    public DateTime CreatedAt { get; set; }
}
