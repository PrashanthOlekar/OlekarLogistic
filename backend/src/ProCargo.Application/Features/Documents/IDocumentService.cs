using ProCargo.Application.Abstractions.Storage;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Documents;

/// <summary>KYC and vehicle documents: upload, list, download and admin review.</summary>
public interface IDocumentService
{
    Task<CreatedResource> UploadAsync(UploadDocumentRequest request, FileUpload? file, CancellationToken cancellationToken);

    /// <summary>Users see the documents on their own profile; admins see all (except trip photos).</summary>
    Task<PagedResult<DocumentListItem>> GetPagedAsync(DocumentQuery query, CancellationToken cancellationToken);

    /// <summary>Only the uploader and admins may open a file.</summary>
    Task<DocumentFile> GetFileAsync(long documentId, CancellationToken cancellationToken);

    /// <summary>Admin: verify or reject a document.</summary>
    Task ReviewAsync(long documentId, VerificationRequest request, CancellationToken cancellationToken);
}
