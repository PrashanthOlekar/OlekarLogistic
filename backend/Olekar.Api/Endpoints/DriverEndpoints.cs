using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Olekar.Api.Data;
using Olekar.Api.Services;

namespace Olekar.Api.Endpoints;

public record AdvanceRequest(string Action, decimal? Latitude, decimal? Longitude);
public record VerifyTripOtpRequest(string Kind, string Code);

public static class DriverEndpoints
{
    // Trip steps in order. Each driver action moves the trip exactly one step.
    //   Assigned -> EnRouteToPickup -> AtPickup -(pickup OTP)-> Loaded -> InTransit -> AtDestination -(POD + delivery OTP)-> Delivered
    public static void MapDriverEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/driver").RequireAuthorization(Roles.Driver);

        g.MapGet("/trips", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var d = await CurrentDriver(db, me);
            return await db.Trips.Where(t => t.DriverId == d.DriverId).OrderBy(t => t.Status == "Completed" || t.Status == "Cancelled").ThenByDescending(t => t.CreatedAt)
                .Select(t => new { id = t.TripId, t.TripNumber, t.Status, from = t.Booking.PickupCity!.Name, to = t.Booking.DropCity!.Name, t.Booking.PickupDate, vehicle = t.Vehicle.RegistrationNumber })
                .ToListAsync();
        });

        g.MapGet("/trips/{id:long}", async (long id, ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var d = await CurrentDriver(db, me);
            var t = await db.Trips.Include(x => x.Booking).ThenInclude(b => b.PickupCity).Include(x => x.Booking).ThenInclude(b => b.DropCity)
                .Include(x => x.Vehicle).Include(x => x.Events)
                .FirstOrDefaultAsync(x => x.TripId == id && x.DriverId == d.DriverId) ?? throw ApiException.NotFound("Trip not found.");
            var hasPod = await db.Documents.AnyAsync(doc => doc.EntityType == "Trip" && doc.EntityId == id && doc.DocType == "POD");
            var b = t.Booking;
            return new
            {
                id = t.TripId, t.TripNumber, t.Status, vehicle = t.Vehicle.RegistrationNumber, hasPod,
                booking = new
                {
                    b.BookingNumber, b.GoodsDescription, b.WeightKg, b.PickupDate, b.PickupSlot, b.SpecialInstructions,
                    pickup = new { city = b.PickupCity?.Name, address = b.PickupAddressText, contact = b.PickupContactName, phone = b.PickupContactPhone },
                    drop = new { city = b.DropCity?.Name, address = b.DropAddressText, contact = b.DropContactName, phone = b.DropContactPhone }
                },
                t.PlannedDistanceKm, t.DriverPay,
                events = t.Events.OrderBy(e => e.CreatedAt).Select(e => new { e.EventType, e.Note, e.CreatedAt })
            };
        });

        g.MapPost("/trips/{id:long}/advance", async (long id, AdvanceRequest r, ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var t = await OwnTrip(db, me, id);
            var (from, to, evt) = r.Action switch
            {
                "EnRoute" => (new[] { "Assigned" }, "EnRouteToPickup", "EnRouteToPickup"),
                "ReachedPickup" => (new[] { "Assigned", "EnRouteToPickup" }, "AtPickup", "ReachedPickup"),
                "StartTrip" => (new[] { "Loaded" }, "InTransit", "TripStarted"),
                "ReachedDestination" => (new[] { "InTransit" }, "AtDestination", "ReachedDestination"),
                _ => throw ApiException.BadRequest("Unknown action.")
            };
            if (!from.Contains(t.Status)) throw ApiException.Conflict($"This step isn't available while the trip is {t.Status}.");
            t.Status = to; t.UpdatedAt = DateTime.UtcNow;
            if (to == "AtPickup") t.ReachedPickupAt = DateTime.UtcNow;
            if (to == "InTransit")
            {
                t.StartedAt = DateTime.UtcNow;
                var b = await db.Bookings.FindAsync(t.BookingId);
                b!.Status = "InTransit"; b.UpdatedAt = DateTime.UtcNow;
            }
            if (to == "AtDestination") t.ReachedDropAt = DateTime.UtcNow;
            db.TripEvents.Add(new TripEvent { TripId = t.TripId, EventType = evt, Latitude = r.Latitude, Longitude = r.Longitude, CreatedBy = me.UserId() });
            if (r.Latitude is decimal lat && r.Longitude is decimal lng)
            {
                var v = await db.Vehicles.FindAsync(t.VehicleId);
                v!.LastLatitude = lat; v.LastLongitude = lng; v.LastSeenAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/trips/{id:long}/verify-otp", async (long id, VerifyTripOtpRequest r, ClaimsPrincipal me, OlekarDbContext db, PiiProtector pii) =>
        {
            var t = await OwnTrip(db, me, id);
            var code = (r.Code ?? "").Trim();
            if (r.Kind == "Pickup")
            {
                if (t.Status != "AtPickup") throw ApiException.Conflict("Mark \"Reached pickup\" first.");
                if (code != pii.Unprotect(t.PickupOtpProtected!)) throw ApiException.BadRequest("That code doesn't match. Ask the sender to check it.");
                t.Status = "Loaded"; t.LoadedAt = DateTime.UtcNow;
                db.TripEvents.Add(new TripEvent { TripId = t.TripId, EventType = "PickupOtpVerified", Note = "Goods handed over", CreatedBy = me.UserId() });
            }
            else if (r.Kind == "Delivery")
            {
                if (t.Status != "AtDestination") throw ApiException.Conflict("Mark \"Reached destination\" first.");
                var hasPod = await db.Documents.AnyAsync(d => d.EntityType == "Trip" && d.EntityId == id && d.DocType == "POD");
                if (!hasPod) throw ApiException.BadRequest("Upload the signed delivery receipt (POD) first.");
                if (code != pii.Unprotect(t.DeliveryOtpProtected!)) throw ApiException.BadRequest("That code doesn't match. Ask the receiver to check it.");
                await TripOps2.MarkDeliveredAsync(db, t, me.UserId());
            }
            else throw ApiException.BadRequest("Kind must be Pickup or Delivery.");
            t.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/trips/{id:long}/photo", async (long id, HttpRequest req, ClaimsPrincipal me, OlekarDbContext db, FileStorage files) =>
        {
            var t = await OwnTrip(db, me, id);
            var form = await req.ReadFormAsync();
            var kind = form["kind"].ToString();
            Guard.Require(kind is "PickupPhoto" or "POD", "Kind must be PickupPhoto or POD.");
            if (kind == "POD") Guard.Require(t.Status == "AtDestination", "Upload the POD after you reach the destination.");
            else Guard.Require(t.Status is "AtPickup" or "Loaded", "Upload the goods photo at pickup.");
            var file = form.Files.FirstOrDefault() ?? throw ApiException.BadRequest("Attach a photo.");
            var saved = await files.SaveAsync(file);
            decimal? lat = decimal.TryParse(form["latitude"].ToString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var la) ? la : null;
            decimal? lng = decimal.TryParse(form["longitude"].ToString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var lo) ? lo : null;
            var doc = new Document
            {
                EntityType = "Trip", EntityId = id, DocType = kind, BlobPath = saved.Path, FileName = saved.FileName, ContentType = saved.ContentType,
                SizeBytes = saved.Size, Sha256 = saved.Sha256, UploadedBy = me.UserId(), Latitude = lat, Longitude = lng
            };
            db.Documents.Add(doc);
            await db.SaveChangesAsync();
            db.TripEvents.Add(new TripEvent { TripId = id, EventType = kind == "POD" ? "PodUploaded" : "GoodsPhotoUploaded", DocumentId = doc.DocumentId, CreatedBy = me.UserId(), Latitude = lat, Longitude = lng });
            await db.SaveChangesAsync();
            return Results.Ok(new { documentId = doc.DocumentId });
        }).DisableAntiforgery();
    }

    private static async Task<Driver> CurrentDriver(OlekarDbContext db, ClaimsPrincipal me)
    {
        var uid = me.UserId();
        return await db.Drivers.FirstOrDefaultAsync(d => d.UserId == uid) ?? throw ApiException.Forbidden("Driver profile not found.");
    }

    private static async Task<Trip> OwnTrip(OlekarDbContext db, ClaimsPrincipal me, long id)
    {
        var d = await CurrentDriver(db, me);
        return await db.Trips.FirstOrDefaultAsync(t => t.TripId == id && t.DriverId == d.DriverId) ?? throw ApiException.NotFound("Trip not found.");
    }
}

public static class TripOps2
{
    /// <summary>Delivery confirmed: frees the truck and driver and opens the owner's settlement, which waits for POD approval.</summary>
    public static async Task MarkDeliveredAsync(OlekarDbContext db, Trip t, long actorUserId)
    {
        t.Status = "Delivered"; t.DeliveredAt = DateTime.UtcNow;
        db.TripEvents.Add(new TripEvent { TripId = t.TripId, EventType = "DeliveryOtpVerified", Note = "Goods delivered", CreatedBy = actorUserId });
        var b = await db.Bookings.FindAsync(t.BookingId);
        b!.Status = "Delivered"; b.UpdatedAt = DateTime.UtcNow;
        var v = await db.Vehicles.FindAsync(t.VehicleId);
        v!.AvailabilityStatus = "Available";
        var d = await db.Drivers.FindAsync(t.DriverId);
        d!.DutyStatus = "Available";
        var quote = await db.Quotes.Where(q => q.BookingId == t.BookingId && q.Status == "Accepted").FirstAsync();
        var bank = await db.OwnerBankAccounts.Where(a => a.OwnerId == t.OwnerId && a.IsPrimary && a.IsActive).Select(a => (long?)a.OwnerBankAccountId).FirstOrDefaultAsync();
        var freight = quote.VehicleCost + quote.DriverCost + quote.LoadingCharges;
        db.Settlements.Add(new Settlement
        {
            TripId = t.TripId, OwnerId = t.OwnerId, OwnerBankAccountId = bank, GrossAmount = freight, CommissionAmount = quote.PlatformFee,
            TdsAmount = 0, NetAmount = freight - quote.PlatformFee, Status = "AwaitingPod"
        });
    }
}
