namespace ProCargo.Application.Features.Trips;

/// <summary>A stored trip photo, ready to record.</summary>
public sealed record TripPhoto(
    long TripId,
    string DocType,
    string BlobPath,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    decimal? Latitude,
    decimal? Longitude,
    long UploadedBy);
