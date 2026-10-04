using FluentValidation;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Storage;
using ProCargo.Application.Common;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Documents;

internal sealed class DocumentService(
    IDocumentRepository documents,
    IVehicleRepository vehicles,
    IFileStorage storage,
    ICurrentProfiles profiles,
    ICurrentUser currentUser,
    IAuditTrail auditTrail,
    IValidator<UploadDocumentRequest> uploadValidator,
    IValidator<VerificationRequest> reviewValidator,
    ILogger<DocumentService> logger) : IDocumentService
{
    public async Task<CreatedResource> UploadAsync(UploadDocumentRequest request, FileUpload? file, CancellationToken cancellationToken)
    {
        await uploadValidator.ValidateAndThrowAsync(request, cancellationToken);
        long entityId = await ResolveOwnEntityAsync(request.EntityType, request.EntityId, cancellationToken);

        FileUploadRules.Check(file);
        StoredFile saved = await storage.SaveAsync(file!, cancellationToken);

        // Aadhaar numbers are never stored.
        string? documentNumber = request.DocType == DocumentType.Aadhaar ? null : Text.Clean(request.DocumentNumber);

        long documentId = await documents.CreateAsync(
            new NewDocument(
                request.EntityType,
                entityId,
                request.DocType,
                saved.Path,
                saved.FileName,
                saved.ContentType,
                saved.SizeBytes,
                saved.Sha256,
                documentNumber,
                request.ExpiryDate,
                currentUser.UserId),
            cancellationToken);

        logger.LogInformation("{EntityType} {EntityId} uploaded {DocType} as document {DocumentId}", request.EntityType, entityId, request.DocType, documentId);
        return new CreatedResource(documentId);
    }

    public async Task<PagedResult<DocumentListItem>> GetPagedAsync(DocumentQuery query, CancellationToken cancellationToken)
    {
        DocumentFilter filter = currentUser.Role switch
        {
            Roles.Admin => DocumentFilter.AllExceptTrips,
            Roles.Owner => DocumentFilter.For(DocumentEntity.Owner, (await profiles.GetOwnerAsync(cancellationToken)).OwnerId),
            Roles.Driver => DocumentFilter.For(DocumentEntity.Driver, (await profiles.GetDriverAsync(cancellationToken)).DriverId),
            Roles.Customer => DocumentFilter.For(DocumentEntity.Customer, await profiles.GetCustomerIdAsync(cancellationToken)),
            _ => throw new ForbiddenException("You can't list documents."),
        };

        return await documents.GetPagedAsync(filter, query, cancellationToken);
    }

    public async Task<DocumentFile> GetFileAsync(long documentId, CancellationToken cancellationToken)
    {
        Document document = await documents.GetByIdAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Document not found.");

        bool canOpen = currentUser.IsInRole(Roles.Admin) || document.UploadedBy == currentUser.UserId;
        if (!canOpen)
        {
            throw new ForbiddenException("You can't open this document.");
        }

        Stream content = await storage.OpenReadAsync(document.BlobPath, cancellationToken)
            ?? throw new NotFoundException("The file is missing from storage.");

        return new DocumentFile(content, document.ContentType, document.FileName);
    }

    public async Task ReviewAsync(long documentId, VerificationRequest request, CancellationToken cancellationToken)
    {
        await reviewValidator.ValidateAndThrowAsync(request, cancellationToken);

        string status = request.Approve ? DocumentStatus.Verified : DocumentStatus.Rejected;
        bool found = await documents.ReviewAsync(documentId, status, request.Reason?.Trim(), currentUser.UserId, cancellationToken);
        if (!found)
        {
            throw new NotFoundException("Document not found.");
        }

        await auditTrail.RecordAsync($"Document.{status}", "Document", documentId, new { request.Reason }, cancellationToken);
    }

    /// <summary>
    /// Finds the owner, driver, customer or vehicle row that belongs to the caller,
    /// so nobody can attach documents to someone else's profile.
    /// </summary>
    private async Task<long> ResolveOwnEntityAsync(string entityType, long? entityId, CancellationToken cancellationToken)
    {
        switch (entityType)
        {
            case DocumentEntity.Owner:
                RequireRole(Roles.Owner, "Only lorry owners can upload owner documents.");
                return (await profiles.GetOwnerAsync(cancellationToken)).OwnerId;

            case DocumentEntity.Driver:
                RequireRole(Roles.Driver, "Only drivers can upload driver documents.");
                return (await profiles.GetDriverAsync(cancellationToken)).DriverId;

            case DocumentEntity.Customer:
                RequireRole(Roles.Customer, "Only customers can upload customer documents.");
                return await profiles.GetCustomerIdAsync(cancellationToken);

            case DocumentEntity.Vehicle:
                RequireRole(Roles.Owner, "That vehicle is not yours.");
                Owner owner = await profiles.GetOwnerAsync(cancellationToken);
                Vehicle? vehicle = await vehicles.GetByIdAsync(entityId ?? 0, cancellationToken);
                if (vehicle is null || vehicle.OwnerId != owner.OwnerId)
                {
                    throw new ForbiddenException("That vehicle is not yours.");
                }
                return vehicle.VehicleId;

            default:
                throw new BusinessRuleException("Unknown document owner.");
        }
    }

    private void RequireRole(string role, string message)
    {
        if (!currentUser.IsInRole(role))
        {
            throw new ForbiddenException(message);
        }
    }
}
