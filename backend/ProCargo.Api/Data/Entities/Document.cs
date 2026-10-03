using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// An uploaded file (KYC, vehicle paper, trip photo or POD) and its review status. Table: Documents.
/// </summary>
public class Document
{
    public long DocumentId { get; set; }

    // Owner | Driver | Vehicle | Customer | Trip | Ticket | Invoice
    public string EntityType { get; set; } = "";

    public long EntityId { get; set; }

    // RC | Insurance | ... | POD | PickupPhoto
    public string DocType { get; set; } = "";

    public string BlobPath { get; set; } = "";

    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long SizeBytes { get; set; }

    public string? Sha256 { get; set; }

    public string? DocumentNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    // Pending | Verified | Rejected | Expired
    public string Status { get; set; } = "Pending";

    public string? RejectionReason { get; set; }

    public long? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public long UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Precision(9, 6)]
    public decimal? Latitude { get; set; }

    [Precision(9, 6)]
    public decimal? Longitude { get; set; }
}
