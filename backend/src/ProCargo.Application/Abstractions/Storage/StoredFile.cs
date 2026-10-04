namespace ProCargo.Application.Abstractions.Storage;

/// <summary>Details of a saved file, ready to record in the Documents table.</summary>
public sealed record StoredFile(string Path, string FileName, string ContentType, long SizeBytes, string Sha256);
