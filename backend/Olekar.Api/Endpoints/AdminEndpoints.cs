using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Olekar.Api.Data;
using Olekar.Api.Services;

namespace Olekar.Api.Endpoints;

public record ReviewRequest(bool Approve, string? Reason);
public record AssignRequest(long VehicleId, long DriverId);
public record ReleaseRequest(string? Utr);
public record BlockRequest(bool Block);

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/admin").RequireAuthorization(Roles.Admin);

        // ---------------- dashboard
        g.MapGet("/summary", async (OlekarDbContext db) =>
        {
            var ist = TimeSpan.FromHours(5.5);
            var todayIst = DateTime.UtcNow.Add(ist).Date;
            var todayStartUtc = todayIst - ist;
            var monthStartUtc = new DateTime(todayIst.Year, todayIst.Month, 1) - ist;
            var weekStartUtc = todayStartUtc.AddDays(-6);

            var recent = await db.Bookings.Where(b => b.CreatedAt >= weekStartUtc).Select(b => b.CreatedAt).ToListAsync();
            var last7 = Enumerable.Range(0, 7).Select(i => todayIst.AddDays(i - 6)).Select(day => new
            {
                date = DateOnly.FromDateTime(day),
                count = recent.Count(c => c.Add(ist).Date == day)
            });
            var routes = await db.Bookings.Where(b => b.Status != "Cancelled" && b.CreatedAt >= monthStartUtc.AddMonths(-2))
                .GroupBy(b => new { from = b.PickupCity!.Name, to = b.DropCity!.Name })
                .Select(x => new { route = x.Key.from + " → " + x.Key.to, count = x.Count() })
                .OrderByDescending(x => x.count).Take(6).ToListAsync();
            var cities = await db.Bookings.Where(b => b.Status != "Cancelled")
                .GroupBy(b => b.PickupCity!.Name).Select(x => new { city = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count).Take(7).ToListAsync();
            var vehicles = await db.Vehicles.Where(v => v.VerificationStatus == "Approved")
                .GroupBy(v => v.AvailabilityStatus).Select(x => new { status = x.Key, count = x.Count() }).ToListAsync();

            return new
            {
                bookingsToday = await db.Bookings.CountAsync(b => b.CreatedAt >= todayStartUtc),
                activeTrips = await db.Trips.CountAsync(t => t.Status != "Completed" && t.Status != "Cancelled" && t.Status != "Delivered"),
                completedThisMonth = await db.Trips.CountAsync(t => t.Status == "Completed" && t.PodApprovedAt >= monthStartUtc),
                awaitingPayment = await db.Bookings.CountAsync(b => b.Status == "Quoted"),
                awaitingTruck = await db.Bookings.CountAsync(b => b.Status == "Confirmed"),
                revenueThisMonth = await db.Payments.Where(p => p.Status == "Captured" && p.PaidAt >= monthStartUtc).SumAsync(p => (decimal?)p.Amount) ?? 0,
                commissionThisMonth = await db.Settlements.Where(s => s.CreatedAt >= monthStartUtc && s.Status != "OnHold").SumAsync(s => (decimal?)s.CommissionAmount) ?? 0,
                pendingApprovals = await db.Owners.CountAsync(o => o.KycStatus == "Pending") + await db.Drivers.CountAsync(d => d.KycStatus == "Pending")
                                   + await db.Vehicles.CountAsync(v => v.VerificationStatus == "Pending"),
                pendingDocuments = await db.Documents.CountAsync(d => d.Status == "Pending" && d.EntityType != "Trip"),
                podToApprove = await db.Trips.CountAsync(t => t.Status == "Delivered"),
                settlementsToRelease = await db.Settlements.CountAsync(s => s.Status == "Approved"),
                last7, routes, cities, vehicles
            };
        });

        // ---------------- approvals (owners, drivers, vehicles)
        g.MapGet("/approvals", async (OlekarDbContext db) => new
        {
            owners = await db.Owners.Where(o => o.KycStatus == "Pending").OrderBy(o => o.CreatedAt).Select(o => new
            {
                id = o.OwnerId, name = o.User.FullName, mobile = o.User.Mobile, o.BusinessName, o.PanLast4, o.AadhaarLast4, o.CreatedAt,
                bank = db.OwnerBankAccounts.Where(a => a.OwnerId == o.OwnerId && a.IsPrimary).Select(a => a.IFSC + " ••" + a.AccountLast4).FirstOrDefault(),
                documents = db.Documents.Where(d => d.EntityType == "Owner" && d.EntityId == o.OwnerId).Select(d => new { id = d.DocumentId, d.DocType, d.Status }).ToList()
            }).ToListAsync(),
            drivers = await db.Drivers.Where(d => d.KycStatus == "Pending").OrderBy(d => d.CreatedAt).Select(d => new
            {
                id = d.DriverId, name = d.User.FullName, mobile = d.User.Mobile, d.LicenceNumber, d.LicenceClass, d.LicenceExpiry, d.CreatedAt,
                owner = d.Owner != null ? d.Owner.User.FullName : null,
                documents = db.Documents.Where(x => x.EntityType == "Driver" && x.EntityId == d.DriverId).Select(x => new { id = x.DocumentId, x.DocType, x.Status }).ToList()
            }).ToListAsync(),
            vehicles = await db.Vehicles.Where(v => v.VerificationStatus == "Pending").OrderBy(v => v.CreatedAt).Select(v => new
            {
                id = v.VehicleId, v.RegistrationNumber, vehicleType = v.VehicleType.Name, v.CapacityKg, owner = v.Owner.User.FullName, v.CreatedAt,
                documents = db.Documents.Where(x => x.EntityType == "Vehicle" && x.EntityId == v.VehicleId).Select(x => new { id = x.DocumentId, x.DocType, x.Status, x.ExpiryDate }).ToList()
            }).ToListAsync()
        });

        g.MapPost("/approvals/{kind}/{id:long}", async (string kind, long id, ReviewRequest r, ClaimsPrincipal me, OlekarDbContext db, HttpContext http) =>
        {
            Guard.Require(r.Approve || !string.IsNullOrWhiteSpace(r.Reason), "Give a reason so the applicant knows what to fix.");
            var admin = me.UserId();
            var now = DateTime.UtcNow;
            switch (kind)
            {
                case "owner":
                    var o = await db.Owners.Include(x => x.User).FirstOrDefaultAsync(x => x.OwnerId == id) ?? throw ApiException.NotFound("Owner not found.");
                    o.KycStatus = r.Approve ? "Approved" : "Rejected"; o.RejectionReason = r.Approve ? null : r.Reason;
                    o.VerifiedBy = admin; o.VerifiedAt = now; o.UpdatedAt = now;
                    if (r.Approve && o.User.Status == "PendingKyc") o.User.Status = "Active";
                    break;
                case "driver":
                    var d = await db.Drivers.Include(x => x.User).FirstOrDefaultAsync(x => x.DriverId == id) ?? throw ApiException.NotFound("Driver not found.");
                    d.KycStatus = r.Approve ? "Approved" : "Rejected"; d.RejectionReason = r.Approve ? null : r.Reason;
                    d.VerifiedBy = admin; d.VerifiedAt = now; d.UpdatedAt = now;
                    if (r.Approve) { d.DutyStatus = "Available"; if (d.User.Status == "PendingKyc") d.User.Status = "Active"; }
                    break;
                case "vehicle":
                    var v = await db.Vehicles.FindAsync(id) ?? throw ApiException.NotFound("Vehicle not found.");
                    v.VerificationStatus = r.Approve ? "Approved" : "Rejected"; v.UpdatedAt = now;
                    break;
                default:
                    throw ApiException.BadRequest("Kind must be owner, driver or vehicle.");
            }
            Audit.Log(db, http, $"{kind}.{(r.Approve ? "Approved" : "Rejected")}", kind, id, new { r.Reason });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---------------- documents
        g.MapGet("/documents", async (string? status, OlekarDbContext db) =>
            await db.Documents.Where(d => d.EntityType != "Trip" && (status == null || d.Status == status))
                .OrderByDescending(d => d.UploadedAt).Take(200)
                .Select(d => new { id = d.DocumentId, d.EntityType, d.EntityId, d.DocType, d.FileName, d.DocumentNumber, d.ExpiryDate, d.Status, d.RejectionReason, d.UploadedAt })
                .ToListAsync());

        g.MapPost("/documents/{id:long}/review", async (long id, ReviewRequest r, ClaimsPrincipal me, OlekarDbContext db, HttpContext http) =>
        {
            Guard.Require(r.Approve || !string.IsNullOrWhiteSpace(r.Reason), "Give a reason so the uploader knows what to fix.");
            var doc = await db.Documents.FindAsync(id) ?? throw ApiException.NotFound("Document not found.");
            doc.Status = r.Approve ? "Verified" : "Rejected"; doc.RejectionReason = r.Approve ? null : r.Reason;
            doc.ReviewedBy = me.UserId(); doc.ReviewedAt = DateTime.UtcNow;
            Audit.Log(db, http, $"Document.{doc.Status}", "Document", id, new { r.Reason });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---------------- bookings & manual assignment
        g.MapGet("/bookings", async (string? status, OlekarDbContext db) =>
            await db.Bookings.Where(b => status == null || b.Status == status).OrderByDescending(b => b.CreatedAt).Take(200)
                .Select(b => new
                {
                    id = b.BookingId, b.BookingNumber, b.Status, customer = b.Customer.CompanyName ?? b.Customer.User.FullName, customerMobile = b.Customer.User.Mobile,
                    from = b.PickupCity!.Name, to = b.DropCity!.Name, b.PickupDate, b.WeightKg, vehicleType = b.VehicleType.Name, b.VehicleTypeId, b.CreatedAt,
                    total = b.Quotes.OrderByDescending(q => q.CreatedAt).Select(q => (decimal?)q.TotalAmount).FirstOrDefault()
                }).ToListAsync());

        g.MapGet("/bookings/{id:long}/assignable", async (long id, OlekarDbContext db) =>
        {
            var b = await db.Bookings.FindAsync(id) ?? throw ApiException.NotFound("Booking not found.");
            return await db.Vehicles
                .Where(v => v.VehicleTypeId == b.VehicleTypeId && v.CapacityKg >= b.WeightKg && v.VerificationStatus == "Approved"
                            && v.AvailabilityStatus == "Available" && v.Owner.KycStatus == "Approved")
                .Select(v => new
                {
                    id = v.VehicleId, v.RegistrationNumber, owner = v.Owner.User.FullName, v.CurrentDriverId,
                    drivers = v.Owner.Drivers.Where(d => d.KycStatus == "Approved" && d.DutyStatus == "Available")
                        .Select(d => new { id = d.DriverId, name = d.User.FullName }).ToList()
                }).ToListAsync();
        });

        g.MapPost("/bookings/{id:long}/assign", async (long id, AssignRequest r, ClaimsPrincipal me, OlekarDbContext db, PiiProtector pii, HttpContext http) =>
        {
            var trip = await TripOps.AssignAsync(db, pii, id, r.VehicleId, r.DriverId, null, me.UserId());
            Audit.Log(db, http, "Booking.AssignedByAdmin", "Booking", id, new { r.VehicleId, r.DriverId });
            await db.SaveChangesAsync();
            return Results.Ok(new { tripId = trip.TripId, trip.TripNumber });
        });

        // ---------------- trips & POD
        g.MapGet("/trips", async (string? status, OlekarDbContext db) =>
            await db.Trips.Where(t => status == null || t.Status == status).OrderByDescending(t => t.CreatedAt).Take(200)
                .Select(t => new
                {
                    id = t.TripId, t.TripNumber, t.Status, bookingNumber = t.Booking.BookingNumber, from = t.Booking.PickupCity!.Name, to = t.Booking.DropCity!.Name,
                    vehicle = t.Vehicle.RegistrationNumber, driver = t.Driver.User.FullName, driverMobile = t.Driver.User.Mobile, owner = t.Owner.User.FullName,
                    t.CreatedAt, t.DeliveredAt,
                    pod = db.Documents.Where(d => d.EntityType == "Trip" && d.EntityId == t.TripId && d.DocType == "POD").OrderByDescending(d => d.UploadedAt).Select(d => (long?)d.DocumentId).FirstOrDefault(),
                    lastEvent = t.Events.OrderByDescending(e => e.CreatedAt).Select(e => e.EventType).FirstOrDefault()
                }).ToListAsync());

        g.MapPost("/trips/{id:long}/approve-pod", async (long id, ClaimsPrincipal me, OlekarDbContext db, HttpContext http) =>
        {
            var t = await db.Trips.Include(x => x.Booking).ThenInclude(b => b.PickupCity).Include(x => x.Booking).ThenInclude(b => b.DropCity)
                .Include(x => x.Booking).ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(x => x.TripId == id) ?? throw ApiException.NotFound("Trip not found.");
            Guard.Require(t.Status == "Delivered", "Only delivered trips can have their POD approved.");
            var now = DateTime.UtcNow;
            t.Status = "Completed"; t.PodApprovedBy = me.UserId(); t.PodApprovedAt = now; t.UpdatedAt = now;
            t.Booking.Status = "Completed"; t.Booking.UpdatedAt = now;
            db.TripEvents.Add(new TripEvent { TripId = id, EventType = "PodApproved", CreatedBy = me.UserId() });
            var s = await db.Settlements.FirstOrDefaultAsync(x => x.TripId == id);
            if (s is not null) { s.Status = "Approved"; s.ApprovedBy = me.UserId(); }
            foreach (var doc in await db.Documents.Where(d => d.EntityType == "Trip" && d.EntityId == id && d.DocType == "POD").ToListAsync())
            { doc.Status = "Verified"; doc.ReviewedBy = me.UserId(); doc.ReviewedAt = now; }

            // GST invoice: intra-state -> CGST + SGST, inter-state -> IGST.
            var quote = await db.Quotes.Where(q => q.BookingId == t.BookingId && q.Status == "Accepted").FirstAsync();
            var seq = (await db.Database.SqlQueryRaw<long>("SELECT CAST(NEXT VALUE FOR dbo.InvoiceNumberSeq AS BIGINT) AS [Value]").ToListAsync()).Single();
            var istNow = now.AddHours(5.5);
            var fyStart = istNow.Month >= 4 ? istNow.Year : istNow.Year - 1;
            var sameState = t.Booking.PickupCity?.State == t.Booking.DropCity?.State;
            var taxable = quote.TotalAmount - quote.TaxAmount;
            db.Invoices.Add(new Invoice
            {
                InvoiceNumber = $"OLK/{fyStart % 100:00}-{(fyStart + 1) % 100:00}/{seq:000000}", BookingId = t.BookingId, CustomerId = t.Booking.CustomerId,
                CustomerGSTIN = t.Booking.Customer.GSTIN, PlaceOfSupply = t.Booking.PickupCity?.State ?? "Karnataka",
                TaxableAmount = taxable,
                CGST = sameState ? Math.Round(quote.TaxAmount / 2, 2) : 0, SGST = sameState ? quote.TaxAmount - Math.Round(quote.TaxAmount / 2, 2) : 0,
                IGST = sameState ? 0 : quote.TaxAmount, TotalAmount = quote.TotalAmount
            });
            Audit.Log(db, http, "Trip.PodApproved", "Trip", id);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---------------- money
        g.MapGet("/payments", async (OlekarDbContext db) =>
            await db.Payments.OrderByDescending(p => p.CreatedAt).Take(200)
                .Join(db.Bookings, p => p.BookingId, b => b.BookingId, (p, b) => new
                {
                    id = p.PaymentId, b.BookingNumber, customer = b.Customer.CompanyName ?? b.Customer.User.FullName,
                    p.Amount, p.Method, p.Gateway, p.Status, p.PaidAt, p.GatewayPaymentId
                }).ToListAsync());

        g.MapGet("/settlements", async (string? status, OlekarDbContext db) =>
            await db.Settlements.Where(s => status == null || s.Status == status).OrderByDescending(s => s.CreatedAt).Take(200)
                .Select(s => new
                {
                    id = s.SettlementId, trip = s.Trip.TripNumber, owner = s.Owner.BusinessName ?? s.Owner.User.FullName,
                    bank = db.OwnerBankAccounts.Where(a => a.OwnerBankAccountId == s.OwnerBankAccountId).Select(a => a.IFSC + " ••" + a.AccountLast4).FirstOrDefault(),
                    s.GrossAmount, s.CommissionAmount, s.TdsAmount, s.NetAmount, s.Status, s.UTR, s.ReleasedAt, s.CreatedAt
                }).ToListAsync());

        // Records a payout you have sent from the bank or the gateway's payout dashboard.
        g.MapPost("/settlements/{id:long}/release", async (long id, ReleaseRequest r, ClaimsPrincipal me, OlekarDbContext db, HttpContext http) =>
        {
            var s = await db.Settlements.FindAsync(id) ?? throw ApiException.NotFound("Settlement not found.");
            Guard.Require(s.Status == "Approved", "Approve the trip's POD before releasing the payout.");
            Guard.Require(!string.IsNullOrWhiteSpace(r.Utr), "Enter the bank transfer reference (UTR).");
            s.Status = "Released"; s.UTR = r.Utr!.Trim(); s.ReleasedBy = me.UserId(); s.ReleasedAt = DateTime.UtcNow;
            Audit.Log(db, http, "Settlement.Released", "Settlement", id, new { s.NetAmount, s.UTR });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---------------- users
        g.MapGet("/users", async (string? role, string? q, OlekarDbContext db) =>
            await db.Users.Where(u => (role == null || u.Role == role) && (q == null || u.FullName.Contains(q) || u.Mobile.Contains(q)))
                .OrderByDescending(u => u.CreatedAt).Take(200)
                .Select(u => new { id = u.UserId, u.Role, u.FullName, u.Mobile, u.Email, u.Status, u.CreatedAt, u.LastLoginAt })
                .ToListAsync());

        g.MapPost("/users/{id:long}/block", async (long id, BlockRequest r, ClaimsPrincipal me, OlekarDbContext db, HttpContext http) =>
        {
            Guard.Require(id != me.UserId(), "You can't block your own account.");
            var u = await db.Users.FindAsync(id) ?? throw ApiException.NotFound("User not found.");
            u.Status = r.Block ? "Blocked" : "Active"; u.UpdatedAt = DateTime.UtcNow;
            Audit.Log(db, http, r.Block ? "User.Blocked" : "User.Unblocked", "User", id);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapGet("/audit", async (OlekarDbContext db) =>
            await db.AuditLogs.OrderByDescending(a => a.CreatedAt).Take(100)
                .Select(a => new { a.Action, a.EntityType, a.EntityId, a.ActorUserId, a.NewValues, a.CreatedAt }).ToListAsync());
    }
}
