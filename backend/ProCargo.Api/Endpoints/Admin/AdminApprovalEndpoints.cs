using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Contracts;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;
using ProCargo.Api.Services;

namespace ProCargo.Api.Endpoints.Admin;

/// <summary>
/// KYC checks for owners, drivers and vehicles, and review of uploaded documents.
///   GET  /api/admin/approvals                    everything waiting for a decision
///   POST /api/admin/approvals/{kind}/{id}        kind = owner | driver | vehicle
///   GET  /api/admin/documents?status=Pending     uploaded documents
///   POST /api/admin/documents/{id}/review        verify or reject one document
/// </summary>
public static class AdminApprovalEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/admin").RequireAuthorization(Roles.Admin);

        group.MapGet("/approvals", ListPendingApprovalsAsync);
        group.MapPost("/approvals/{kind}/{id:long}", ReviewApplicationAsync);

        group.MapGet("/documents", ListDocumentsAsync);
        group.MapPost("/documents/{id:long}/review", ReviewDocumentAsync);
    }

    // ---------------------------------------------------------------- applications

    private static async Task<IResult> ListPendingApprovalsAsync(ProCargoDbContext db)
    {
        var owners = await db.Owners
            .Where(owner => owner.KycStatus == KycStatus.Pending)
            .OrderBy(owner => owner.CreatedAt)
            .Select(owner => new
            {
                id = owner.OwnerId,
                name = owner.User.FullName,
                mobile = owner.User.Mobile,
                owner.BusinessName,
                owner.PanLast4,
                owner.AadhaarLast4,
                owner.CreatedAt,
                bank = db.OwnerBankAccounts
                    .Where(account => account.OwnerId == owner.OwnerId && account.IsPrimary)
                    .Select(account => account.IFSC + " ••" + account.AccountLast4)
                    .FirstOrDefault(),
                documents = db.Documents
                    .Where(document => document.EntityType == DocumentEntity.Owner && document.EntityId == owner.OwnerId)
                    .Select(document => new { id = document.DocumentId, document.DocType, document.Status })
                    .ToList(),
            })
            .ToListAsync();

        var drivers = await db.Drivers
            .Where(driver => driver.KycStatus == KycStatus.Pending)
            .OrderBy(driver => driver.CreatedAt)
            .Select(driver => new
            {
                id = driver.DriverId,
                name = driver.User.FullName,
                mobile = driver.User.Mobile,
                driver.LicenceNumber,
                driver.LicenceClass,
                driver.LicenceExpiry,
                driver.CreatedAt,
                owner = driver.Owner != null ? driver.Owner.User.FullName : null,
                documents = db.Documents
                    .Where(document => document.EntityType == DocumentEntity.Driver && document.EntityId == driver.DriverId)
                    .Select(document => new { id = document.DocumentId, document.DocType, document.Status })
                    .ToList(),
            })
            .ToListAsync();

        var vehicles = await db.Vehicles
            .Where(vehicle => vehicle.VerificationStatus == VehicleVerification.Pending)
            .OrderBy(vehicle => vehicle.CreatedAt)
            .Select(vehicle => new
            {
                id = vehicle.VehicleId,
                vehicle.RegistrationNumber,
                vehicleType = vehicle.VehicleType.Name,
                vehicle.CapacityKg,
                owner = vehicle.Owner.User.FullName,
                vehicle.CreatedAt,
                documents = db.Documents
                    .Where(document => document.EntityType == DocumentEntity.Vehicle && document.EntityId == vehicle.VehicleId)
                    .Select(document => new { id = document.DocumentId, document.DocType, document.Status, document.ExpiryDate })
                    .ToList(),
            })
            .ToListAsync();

        return Results.Ok(new { owners, drivers, vehicles });
    }

    private static async Task<IResult> ReviewApplicationAsync(
        string kind,
        long id,
        ReviewRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        HttpContext http)
    {
        Guard.Require(request.Approve || !string.IsNullOrWhiteSpace(request.Reason), "Give a reason so the applicant knows what to fix.");
        long adminUserId = principal.GetUserId();

        switch (kind)
        {
            case "owner":
                await ReviewOwnerAsync(db, id, request, adminUserId);
                break;

            case "driver":
                await ReviewDriverAsync(db, id, request, adminUserId);
                break;

            case "vehicle":
                await ReviewVehicleAsync(db, id, request);
                break;

            default:
                throw ApiException.BadRequest("Kind must be owner, driver or vehicle.");
        }

        string decision = request.Approve ? "Approved" : "Rejected";
        AuditLogger.Log(db, http, $"{kind}.{decision}", kind, id, new { request.Reason });
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task ReviewOwnerAsync(ProCargoDbContext db, long ownerId, ReviewRequest request, long adminUserId)
    {
        Owner owner = await db.Owners
            .Include(row => row.User)
            .FirstOrDefaultAsync(row => row.OwnerId == ownerId)
            ?? throw ApiException.NotFound("Owner not found.");

        DateTime now = DateTime.UtcNow;
        owner.KycStatus = request.Approve ? KycStatus.Approved : KycStatus.Rejected;
        owner.RejectionReason = request.Approve ? null : request.Reason;
        owner.VerifiedBy = adminUserId;
        owner.VerifiedAt = now;
        owner.UpdatedAt = now;

        if (request.Approve && owner.User.Status == UserStatus.PendingKyc)
        {
            owner.User.Status = UserStatus.Active;
        }
    }

    private static async Task ReviewDriverAsync(ProCargoDbContext db, long driverId, ReviewRequest request, long adminUserId)
    {
        Driver driver = await db.Drivers
            .Include(row => row.User)
            .FirstOrDefaultAsync(row => row.DriverId == driverId)
            ?? throw ApiException.NotFound("Driver not found.");

        DateTime now = DateTime.UtcNow;
        driver.KycStatus = request.Approve ? KycStatus.Approved : KycStatus.Rejected;
        driver.RejectionReason = request.Approve ? null : request.Reason;
        driver.VerifiedBy = adminUserId;
        driver.VerifiedAt = now;
        driver.UpdatedAt = now;

        if (request.Approve)
        {
            driver.DutyStatus = DutyStatus.Available;
            if (driver.User.Status == UserStatus.PendingKyc)
            {
                driver.User.Status = UserStatus.Active;
            }
        }
    }

    private static async Task ReviewVehicleAsync(ProCargoDbContext db, long vehicleId, ReviewRequest request)
    {
        Vehicle vehicle = await db.Vehicles.FindAsync(vehicleId)
            ?? throw ApiException.NotFound("Vehicle not found.");

        vehicle.VerificationStatus = request.Approve ? VehicleVerification.Approved : VehicleVerification.Rejected;
        vehicle.UpdatedAt = DateTime.UtcNow;
    }

    // ---------------------------------------------------------------- documents

    private static async Task<IResult> ListDocumentsAsync(string? status, ProCargoDbContext db)
    {
        var documents = await db.Documents
            .Where(document => document.EntityType != DocumentEntity.Trip)
            .Where(document => status == null || document.Status == status)
            .OrderByDescending(document => document.UploadedAt)
            .Take(200)
            .Select(document => new
            {
                id = document.DocumentId,
                document.EntityType,
                document.EntityId,
                document.DocType,
                document.FileName,
                document.DocumentNumber,
                document.ExpiryDate,
                document.Status,
                document.RejectionReason,
                document.UploadedAt,
            })
            .ToListAsync();

        return Results.Ok(documents);
    }

    private static async Task<IResult> ReviewDocumentAsync(
        long id,
        ReviewRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        HttpContext http)
    {
        Guard.Require(request.Approve || !string.IsNullOrWhiteSpace(request.Reason), "Give a reason so the uploader knows what to fix.");

        Document document = await db.Documents.FindAsync(id)
            ?? throw ApiException.NotFound("Document not found.");

        document.Status = request.Approve ? DocumentStatus.Verified : DocumentStatus.Rejected;
        document.RejectionReason = request.Approve ? null : request.Reason;
        document.ReviewedBy = principal.GetUserId();
        document.ReviewedAt = DateTime.UtcNow;

        AuditLogger.Log(db, http, $"Document.{document.Status}", "Document", id, new { request.Reason });
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}
