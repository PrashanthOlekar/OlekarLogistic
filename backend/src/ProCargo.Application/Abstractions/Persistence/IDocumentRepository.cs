using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Documents;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IDocumentRepository
{
    Task<long> CreateAsync(NewDocument document, CancellationToken cancellationToken);

    Task<Document?> GetByIdAsync(long documentId, CancellationToken cancellationToken);

    Task<PagedResult<DocumentListItem>> GetPagedAsync(DocumentFilter filter, DocumentQuery query, CancellationToken cancellationToken);

    /// <summary>Returns false when the document doesn't exist.</summary>
    Task<bool> ReviewAsync(long documentId, string status, string? reason, long reviewedBy, CancellationToken cancellationToken);
}
