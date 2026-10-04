namespace ProCargo.Application.Abstractions.Storage;

/// <summary>Private storage for uploaded files (local folder now, Azure Blob Storage later).</summary>
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(FileUpload file, CancellationToken cancellationToken);

    /// <summary>Opens a stored file, or returns null if it is missing.</summary>
    Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken);
}
