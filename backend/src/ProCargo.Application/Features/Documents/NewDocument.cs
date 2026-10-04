namespace ProCargo.Application.Features.Documents;

/// <summary>A stored file and its details, ready to record.</summary>
public sealed record NewDocument(
    string EntityType,
    long EntityId,
    string DocType,
    string BlobPath,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string? DocumentNumber,
    DateOnly? ExpiryDate,
    long UploadedBy);
