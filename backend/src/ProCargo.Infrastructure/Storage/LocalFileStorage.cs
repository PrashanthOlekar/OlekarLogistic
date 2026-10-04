using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ProCargo.Application.Abstractions.Storage;

namespace ProCargo.Infrastructure.Storage;

/// <summary>
/// Saves uploads (KYC documents, vehicle papers, POD photos) to a private folder on disk.
/// For production, replace this one class with an Azure Blob Storage version.
/// </summary>
internal sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootFolder;

    public LocalFileStorage(IOptions<StorageSettings> settings, IHostEnvironment environment)
    {
        _rootFolder = Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.Value.LocalFolder));
        Directory.CreateDirectory(_rootFolder);
    }

    public async Task<StoredFile> SaveAsync(FileUpload file, CancellationToken cancellationToken)
    {
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Grouped by month with a random name, e.g. 2026/10/3f2a...e1.pdf
        string relativePath = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        string fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using (FileStream output = File.Create(fullPath))
        {
            byte[] buffer = new byte[81920];
            int read;
            while ((read = await file.Content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                sha.AppendData(buffer, 0, read);
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }

        string safeName = Path.GetFileName(file.FileName);
        return new StoredFile(relativePath, safeName, file.ContentType, file.Length, Convert.ToHexString(sha.GetHashAndReset()));
    }

    public Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        string fullPath = GetFullPath(path);
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    /// <summary>Resolves a stored path and refuses anything that would leave the uploads folder.</summary>
    private string GetFullPath(string relativePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_rootFolder, relativePath));
        if (!fullPath.StartsWith(_rootFolder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }
        return fullPath;
    }
}
