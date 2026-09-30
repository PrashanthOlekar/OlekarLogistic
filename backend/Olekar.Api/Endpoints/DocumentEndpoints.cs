using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Olekar.Api.Data;
using Olekar.Api.Services;

namespace Olekar.Api.Endpoints;

public static class DocumentEndpoints
{
    private static readonly Dictionary<string, string[]> DocTypesByEntity = new()
    {
        ["Owner"] = new[] { "Aadhaar", "PAN", "CancelledCheque", "GST", "Other" },
        ["Driver"] = new[] { "Licence", "Aadhaar", "DriverPhoto", "Other" },
        ["Vehicle"] = new[] { "RC", "Insurance", "Fitness", "Permit", "PUC", "Other" },
        ["Customer"] = new[] { "GST", "Other" }
    };

    public static void MapDocumentEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/documents").RequireAuthorization();

        // Upload a KYC or vehicle document. Form fields: entityType, entityId (vehicles only), docType, documentNumber?, expiryDate?, file.
        g.MapPost("/", async (HttpRequest req, ClaimsPrincipal me, OlekarDbContext db, FileStorage files) =>
        {
            var form = await req.ReadFormAsync();
            var entityType = form["entityType"].ToString();
            var docType = form["docType"].ToString();
            Guard.Require(DocTypesByEntity.TryGetValue(entityType, out var allowed) && allowed.Contains(docType), "Choose a valid document type.");
            var entityId = await ResolveOwnEntity(db, me, entityType, form["entityId"].ToString());
            DateOnly? expiry = DateOnly.TryParse(form["expiryDate"].ToString(), out var e) ? e : null;
            var file = form.Files.FirstOrDefault() ?? throw ApiException.BadRequest("Attach the document.");
            var saved = await files.SaveAsync(file);
            var number = form["documentNumber"].ToString();
            var doc = new Document
            {
                EntityType = entityType, EntityId = entityId, DocType = docType, BlobPath = saved.Path, FileName = saved.FileName,
                ContentType = saved.ContentType, SizeBytes = saved.Size, Sha256 = saved.Sha256, ExpiryDate = expiry,
                DocumentNumber = string.IsNullOrWhiteSpace(number) || docType == "Aadhaar" ? null : number.Trim(),   // never keep Aadhaar numbers
                UploadedBy = me.UserId(), Status = "Pending"
            };
            db.Documents.Add(doc);
            await db.SaveChangesAsync();
            return Results.Ok(new { id = doc.DocumentId });
        }).DisableAntiforgery();

        // Documents for the signed-in user's own profile (owner, driver or customer).
        g.MapGet("/mine", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var role = me.FindFirstValue(ClaimTypes.Role);
            if (role is not (Roles.Owner or Roles.Driver or Roles.Customer)) return Results.Ok(Array.Empty<object>());
            var entityId = await ResolveOwnEntity(db, me, role!, "");
            var docs = await db.Documents.Where(d => d.EntityType == role && d.EntityId == entityId).OrderByDescending(d => d.UploadedAt)
                .Select(d => new { id = d.DocumentId, d.DocType, d.FileName, d.Status, d.RejectionReason, d.ExpiryDate, d.UploadedAt }).ToListAsync();
            return Results.Ok(docs);
        });

        // Download: admins, or the person who uploaded it.
        g.MapGet("/{id:long}/file", async (long id, ClaimsPrincipal me, OlekarDbContext db, FileStorage files) =>
        {
            var doc = await db.Documents.FindAsync(id) ?? throw ApiException.NotFound("Document not found.");
            if (!me.IsInRole(Roles.Admin) && doc.UploadedBy != me.UserId()) throw ApiException.Forbidden("You can't open this document.");
            var path = files.FullPath(doc.BlobPath);
            if (!File.Exists(path)) throw ApiException.NotFound("The file is missing from storage.");
            return Results.File(path, doc.ContentType, doc.FileName);
        });
    }

    /// <summary>Returns the id of the owner/driver/customer/vehicle row that belongs to the caller.</summary>
    private static async Task<long> ResolveOwnEntity(OlekarDbContext db, ClaimsPrincipal me, string entityType, string entityIdRaw)
    {
        var uid = me.UserId();
        switch (entityType)
        {
            case "Owner":
                return await db.Owners.Where(o => o.UserId == uid).Select(o => (long?)o.OwnerId).FirstOrDefaultAsync() ?? throw ApiException.Forbidden("Only lorry owners can upload owner documents.");
            case "Driver":
                return await db.Drivers.Where(d => d.UserId == uid).Select(d => (long?)d.DriverId).FirstOrDefaultAsync() ?? throw ApiException.Forbidden("Only drivers can upload driver documents.");
            case "Customer":
                return await db.Customers.Where(c => c.UserId == uid).Select(c => (long?)c.CustomerId).FirstOrDefaultAsync() ?? throw ApiException.Forbidden("Only customers can upload customer documents.");
            case "Vehicle":
                Guard.Require(long.TryParse(entityIdRaw, out var vid), "Choose the vehicle.");
                var ok = await db.Vehicles.AnyAsync(v => v.VehicleId == vid && v.Owner.UserId == uid);
                if (!ok) throw ApiException.Forbidden("That vehicle is not yours.");
                return vid;
            default:
                throw ApiException.BadRequest("Unknown document owner.");
        }
    }
}
