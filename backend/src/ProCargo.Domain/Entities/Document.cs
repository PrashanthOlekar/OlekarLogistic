namespace ProCargo.Domain.Entities;

/// <summary>An uploaded file (dbo.Documents). The file itself lives in storage at BlobPath.</summary>
public sealed class Document
{
    public long DocumentId { get; set; }

    /// <summary>Owner, Driver, Vehicle, Customer or Trip.</summary>
    public string EntityType { get; set; } = string.Empty;

    public long EntityId { get; set; }

    public string DocType { get; set; } = string.Empty;

    public string BlobPath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string? DocumentNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public long UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }
}
