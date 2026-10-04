using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Approvals;

public sealed class PendingDriver
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string LicenceNumber { get; set; } = string.Empty;

    public string LicenceClass { get; set; } = string.Empty;

    public DateOnly LicenceExpiry { get; set; }

    /// <summary>The owner the driver works for, if any.</summary>
    public string? Owner { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<DocumentChip> Documents { get; set; } = [];
}
