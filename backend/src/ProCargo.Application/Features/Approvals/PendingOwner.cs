using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Approvals;

public sealed class PendingOwner
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string? BusinessName { get; set; }

    public string? PanLast4 { get; set; }

    public string? AadhaarLast4 { get; set; }

    /// <summary>IFSC and the last 4 digits of the payout account.</summary>
    public string? Bank { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<DocumentChip> Documents { get; set; } = [];
}
