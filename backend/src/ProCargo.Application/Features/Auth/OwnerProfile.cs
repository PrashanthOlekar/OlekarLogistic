namespace ProCargo.Application.Features.Auth;

public sealed class OwnerProfile
{
    public long OwnerId { get; set; }

    public string? BusinessName { get; set; }

    public string KycStatus { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public string? PanLast4 { get; set; }

    public string? AadhaarLast4 { get; set; }
}
