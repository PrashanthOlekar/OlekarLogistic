namespace ProCargo.Application.Features.Common;

/// <summary>A document as listed under a vehicle or an applicant.</summary>
public sealed class DocumentChip
{
    public long Id { get; set; }

    public string DocType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateOnly? ExpiryDate { get; set; }
}
