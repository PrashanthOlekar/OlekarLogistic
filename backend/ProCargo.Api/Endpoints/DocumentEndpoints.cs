using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;
using ProCargo.Api.Services;

namespace ProCargo.Api.Endpoints;

/// <summary>
/// KYC and vehicle documents.
///   POST /api/documents             upload (multipart form)
///   GET  /api/documents/mine        documents on my own profile
///   GET  /api/documents/{id}/file   download (admins, or the person who uploaded it)
/// </summary>
public static class DocumentEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/documents").RequireAuthorization();

        group.MapPost("/", UploadDocumentAsync).DisableAntiforgery();
        group.MapGet("/mine", ListMyDocumentsAsync);
        group.MapGet("/{id:long}/file", DownloadFileAsync);
    }

    /// <summary>Form fields: entityType, entityId (vehicles only), docType, documentNumber, expiryDate, file.</summary>
    private static async Task<IResult> UploadDocumentAsync(
        HttpRequest httpRequest,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        FileStorage storage)
    {
        IFormCollection form = await httpRequest.ReadFormAsync();
        string entityType = form["entityType"].ToString();
        string docType = form["docType"].ToString();

        bool isAllowedType = DocumentType.AllowedByEntity.TryGetValue(entityType, out string[]? allowedTypes)
            && allowedTypes.Contains(docType);
        Guard.Require(isAllowedType, "Choose a valid document type.");

        long entityId = await ResolveOwnEntityAsync(db, principal, entityType, form["entityId"].ToString());
        DateOnly? expiryDate = DateOnly.TryParse(form["expiryDate"].ToString(), out DateOnly parsed) ? parsed : null;

        IFormFile file = form.Files.FirstOrDefault() ?? throw ApiException.BadRequest("Attach the document.");
        StoredFile saved = await storage.SaveAsync(file);

        // Aadhaar numbers are never stored.
        string documentNumber = form["documentNumber"].ToString().Trim();
        bool keepNumber = documentNumber.Length > 0 && docType != DocumentType.Aadhaar;

        var document = new Document
        {
            EntityType = entityType,
            EntityId = entityId,
            DocType = docType,
            BlobPath = saved.Path,
            FileName = saved.FileName,
            ContentType = saved.ContentType,
            SizeBytes = saved.SizeBytes,
            Sha256 = saved.Sha256,
            ExpiryDate = expiryDate,
            DocumentNumber = keepNumber ? documentNumber : null,
            UploadedBy = principal.GetUserId(),
            Status = DocumentStatus.Pending,
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        return Results.Ok(new { id = document.DocumentId });
    }

    private static async Task<IResult> ListMyDocumentsAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        string? role = principal.FindFirstValue(ClaimTypes.Role);
        if (role is not (Roles.Owner or Roles.Driver or Roles.Customer))
        {
            return Results.Ok(Array.Empty<object>());
        }

        // The role names match the entity names: Owner, Driver, Customer.
        long entityId = await ResolveOwnEntityAsync(db, principal, role, entityIdText: string.Empty);

        var documents = await db.Documents
            .Where(document => document.EntityType == role && document.EntityId == entityId)
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => new
            {
                id = document.DocumentId,
                document.DocType,
                document.FileName,
                document.Status,
                document.RejectionReason,
                document.ExpiryDate,
                document.UploadedAt,
            })
            .ToListAsync();

        return Results.Ok(documents);
    }

    private static async Task<IResult> DownloadFileAsync(
        long id,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        FileStorage storage)
    {
        Document document = await db.Documents.FindAsync(id)
            ?? throw ApiException.NotFound("Document not found.");

        bool canOpen = principal.IsInRole(Roles.Admin) || document.UploadedBy == principal.GetUserId();
        if (!canOpen)
        {
            throw ApiException.Forbidden("You can't open this document.");
        }

        string fullPath = storage.GetFullPath(document.BlobPath);
        if (!File.Exists(fullPath))
        {
            throw ApiException.NotFound("The file is missing from storage.");
        }

        return Results.File(fullPath, document.ContentType, document.FileName);
    }

    /// <summary>
    /// Finds the owner, driver, customer or vehicle row that belongs to the caller,
    /// so nobody can attach documents to someone else's profile.
    /// </summary>
    private static async Task<long> ResolveOwnEntityAsync(
        ProCargoDbContext db,
        ClaimsPrincipal principal,
        string entityType,
        string entityIdText)
    {
        long userId = principal.GetUserId();

        switch (entityType)
        {
            case DocumentEntity.Owner:
                return await db.Owners
                    .Where(owner => owner.UserId == userId)
                    .Select(owner => (long?)owner.OwnerId)
                    .FirstOrDefaultAsync()
                    ?? throw ApiException.Forbidden("Only lorry owners can upload owner documents.");

            case DocumentEntity.Driver:
                return await db.Drivers
                    .Where(driver => driver.UserId == userId)
                    .Select(driver => (long?)driver.DriverId)
                    .FirstOrDefaultAsync()
                    ?? throw ApiException.Forbidden("Only drivers can upload driver documents.");

            case DocumentEntity.Customer:
                return await db.Customers
                    .Where(customer => customer.UserId == userId)
                    .Select(customer => (long?)customer.CustomerId)
                    .FirstOrDefaultAsync()
                    ?? throw ApiException.Forbidden("Only customers can upload customer documents.");

            case DocumentEntity.Vehicle:
                Guard.Require(long.TryParse(entityIdText, out long vehicleId), "Choose the vehicle.");
                bool isMine = await db.Vehicles.AnyAsync(vehicle => vehicle.VehicleId == vehicleId && vehicle.Owner.UserId == userId);
                if (!isMine)
                {
                    throw ApiException.Forbidden("That vehicle is not yours.");
                }
                return vehicleId;

            default:
                throw ApiException.BadRequest("Unknown document owner.");
        }
    }
}
