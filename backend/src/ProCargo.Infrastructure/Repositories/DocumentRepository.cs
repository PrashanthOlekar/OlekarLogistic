using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Documents;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class DocumentRepository(StoredProcedureExecutor database) : IDocumentRepository
{
    public Task<long> CreateAsync(NewDocument document, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<long>(StoredProcedures.DocumentCreate, new
        {
            document.EntityType,
            document.EntityId,
            document.DocType,
            document.BlobPath,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.Sha256,
            document.DocumentNumber,
            document.ExpiryDate,
            document.UploadedBy,
        }, cancellationToken);

    public Task<Document?> GetByIdAsync(long documentId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Document>(StoredProcedures.DocumentGetById, new { DocumentId = documentId }, cancellationToken);

    public Task<PagedResult<DocumentListItem>> GetPagedAsync(DocumentFilter filter, DocumentQuery query, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("EntityType", filter.EntityType, DbType.String);
        parameters.Add("EntityId", filter.EntityId, DbType.Int64);
        parameters.Add("Status", query.Status, DbType.String);
        parameters.Add("ExcludeTrips", filter.ExcludeTrips, DbType.Boolean);
        parameters.Add("Search", query.CleanSearch, DbType.String);

        return database.QueryPagedAsync<DocumentListItem>(StoredProcedures.DocumentGetPaged, parameters, query.ToPage(), cancellationToken);
    }

    public async Task<bool> ReviewAsync(long documentId, string status, string? reason, long reviewedBy, CancellationToken cancellationToken) =>
        await database.ExecuteScalarAsync<int>(
            StoredProcedures.DocumentReview,
            new { DocumentId = documentId, Status = status, Reason = reason, ReviewedBy = reviewedBy },
            cancellationToken) > 0;
}
