using System.Security.Cryptography;
using ProCargo.Api.Common;

namespace ProCargo.Api.Services;

/// <summary>
/// Saves uploaded files (KYC documents, vehicle papers, POD photos).
/// In development they go to a private folder on disk (App_Data/uploads).
/// For production, change this one class to use Azure Blob Storage.
/// </summary>
public class FileStorage
{
    private const long MaxFileBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic" };

    private readonly string _rootFolder;

    public FileStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        string folder = configuration["Storage:LocalFolder"] ?? "App_Data/uploads";
        _rootFolder = Path.Combine(environment.ContentRootPath, folder);
        Directory.CreateDirectory(_rootFolder);
    }

    public async Task<StoredFile> SaveAsync(IFormFile file)
    {
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        Guard.Require(file.Length > 0, "The file is empty.");
        Guard.Require(file.Length <= MaxFileBytes, "Files must be 10 MB or smaller.");
        Guard.Require(AllowedExtensions.Contains(extension), "Upload a PDF or a photo (JPG, PNG, WEBP, HEIC).");

        // Files are grouped by month and given a random name, e.g. 2026/10/3f2a...e1.pdf
        string relativePath = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        string fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (FileStream output = File.Create(fullPath))
        {
            await file.CopyToAsync(output);
        }

        string sha256 = await ComputeSha256Async(fullPath);
        return new StoredFile(relativePath, Path.GetFileName(file.FileName), file.ContentType, file.Length, sha256);
    }

    public string GetFullPath(string relativePath) => Path.Combine(_rootFolder, relativePath);

    private static async Task<string> ComputeSha256Async(string fullPath)
    {
        await using FileStream input = File.OpenRead(fullPath);
        byte[] hash = await SHA256.HashDataAsync(input);
        return Convert.ToHexString(hash);
    }
}
