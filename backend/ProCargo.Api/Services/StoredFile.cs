namespace ProCargo.Api.Services;

/// <summary>Details of a file saved by FileStorage, ready to record in the Documents table.</summary>
public record StoredFile(string Path, string FileName, string ContentType, long SizeBytes, string Sha256);
