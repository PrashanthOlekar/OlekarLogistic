using ProCargo.Application.Common.Exceptions;

namespace ProCargo.Application.Abstractions.Storage;

/// <summary>Which files may be uploaded: PDFs and photos up to 10 MB.</summary>
public static class FileUploadRules
{
    public const long MaxFileBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlyList<string> AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic"];

    public static void Check(FileUpload? file)
    {
        if (file is null)
        {
            throw new BusinessRuleException("Attach the file.");
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (file.Length <= 0)
        {
            throw new BusinessRuleException("The file is empty.");
        }
        if (file.Length > MaxFileBytes)
        {
            throw new BusinessRuleException("Files must be 10 MB or smaller.");
        }
        if (!AllowedExtensions.Contains(extension))
        {
            throw new BusinessRuleException("Upload a PDF or a photo (JPG, PNG, WEBP, HEIC).");
        }
    }
}
