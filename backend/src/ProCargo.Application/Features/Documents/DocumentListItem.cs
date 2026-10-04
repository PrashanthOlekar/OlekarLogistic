namespace ProCargo.Application.Features.Documents;

public sealed class DocumentListItem
{
    public long Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public long EntityId { get; set; }

    public string DocType { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? DocumentNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public DateTime UploadedAt { get; set; }
}
