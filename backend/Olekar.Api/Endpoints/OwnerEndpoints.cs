using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Olekar.Api.Data;
using Olekar.Api.Services;

namespace Olekar.Api.Endpoints;

public record AddVehicleRequest(string RegistrationNumber, int VehicleTypeId, int CapacityKg, string? MakeModel, short? ManufactureYear, int? HomeCityId);
public record AvailabilityRequest(string Status);
public record SetDriverRequest(long? DriverId);
public record AcceptLoadRequest(long VehicleId, long DriverId);

public static class OwnerEndpoints
{
    public static void MapOwnerEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/owner").RequireAuthorization(Roles.Owner);

        g.MapGet("/summary", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var bank = await db.OwnerBankAccounts.Where(a => a.OwnerId == o.OwnerId && a.IsPrimary && a.IsActive)
                .Select(a => new { a.AccountHolder, a.AccountLast4, a.IFSC, a.BankName, a.PennyDropStatus }).FirstOrDefaultAsync();
            return new
            {
                o.OwnerId, o.BusinessName, o.KycStatus, o.RejectionReason,
                vehicles = await db.Vehicles.CountAsync(v => v.OwnerId == o.OwnerId),
                vehiclesApproved = await db.Vehicles.CountAsync(v => v.OwnerId == o.OwnerId && v.VerificationStatus == "Approved"),
                drivers = await db.Drivers.CountAsync(d => d.OwnerId == o.OwnerId),
                activeTrips = await db.Trips.CountAsync(t => t.OwnerId == o.OwnerId && t.Status != "Completed" && t.Status != "Cancelled"),
                earnedThisMonth = await db.Settlements.Where(s => s.OwnerId == o.OwnerId && s.Status == "Released" && s.ReleasedAt >= monthStart).SumAsync(s => (decimal?)s.NetAmount) ?? 0,
                pendingPayout = await db.Settlements.Where(s => s.OwnerId == o.OwnerId && s.Status != "Released").SumAsync(s => (decimal?)s.NetAmount) ?? 0,
                bank
            };
        });

        // ---- vehicles
        g.MapGet("/vehicles", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            return await db.Vehicles.Where(v => v.OwnerId == o.OwnerId).OrderBy(v => v.RegistrationNumber)
                .Select(v => new
                {
                    id = v.VehicleId, v.RegistrationNumber, vehicleType = v.VehicleType.Name, v.VehicleTypeId, v.CapacityKg, v.MakeModel,
                    v.AvailabilityStatus, v.VerificationStatus, v.CurrentDriverId, driverName = v.CurrentDriver != null ? v.CurrentDriver.User.FullName : null,
                    documents = db.Documents.Where(d => d.EntityType == "Vehicle" && d.EntityId == v.VehicleId)
                        .Select(d => new { id = d.DocumentId, d.DocType, d.Status, d.ExpiryDate }).ToList()
                }).ToListAsync();
        });

        g.MapPost("/vehicles", async (AddVehicleRequest r, ClaimsPrincipal me, OlekarDbContext db, HttpContext http) =>
        {
            var o = await CurrentOwner(db, me);
            var reg = new string((r.RegistrationNumber ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            Guard.Require(System.Text.RegularExpressions.Regex.IsMatch(reg, "^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{4}$"), "Enter the registration number as on the RC, for example KA01AB4521.");
            Guard.Require(!await db.Vehicles.AnyAsync(v => v.RegistrationNumber == reg), "This vehicle is already registered.");
            var vt = await db.VehicleTypes.FindAsync(r.VehicleTypeId) ?? throw ApiException.BadRequest("Choose the vehicle type.");
            Guard.Require(r.CapacityKg > 0 && r.CapacityKg <= 60000, "Enter the vehicle's load capacity in kg.");
            var v = new Vehicle
            {
                OwnerId = o.OwnerId, VehicleTypeId = vt.VehicleTypeId, RegistrationNumber = reg, CapacityKg = r.CapacityKg,
                MakeModel = r.MakeModel, ManufactureYear = r.ManufactureYear, HomeCityId = r.HomeCityId,
                AvailabilityStatus = "Available", VerificationStatus = "Pending"
            };
            db.Vehicles.Add(v);
            await db.SaveChangesAsync();
            Audit.Log(db, http, "Vehicle.Added", "Vehicle", v.VehicleId, new { reg });
            await db.SaveChangesAsync();
            return Results.Created($"/api/owner/vehicles/{v.VehicleId}", new { id = v.VehicleId });
        });

        g.MapPatch("/vehicles/{id:long}/availability", async (long id, AvailabilityRequest r, ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            Guard.Require(r.Status is "Available" or "Busy" or "Maintenance", "Choose Available, Busy or Maintenance.");
            var v = await db.Vehicles.FirstOrDefaultAsync(x => x.VehicleId == id && x.OwnerId == o.OwnerId) ?? throw ApiException.NotFound("Vehicle not found.");
            var onTrip = await db.Trips.AnyAsync(t => t.VehicleId == id && t.Status != "Completed" && t.Status != "Cancelled" && t.Status != "Delivered");
            Guard.Require(!onTrip || r.Status == "Busy", "This vehicle is on a trip. It becomes available again after delivery.");
            v.AvailabilityStatus = r.Status; v.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPatch("/vehicles/{id:long}/driver", async (long id, SetDriverRequest r, ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            var v = await db.Vehicles.FirstOrDefaultAsync(x => x.VehicleId == id && x.OwnerId == o.OwnerId) ?? throw ApiException.NotFound("Vehicle not found.");
            if (r.DriverId is long did)
                Guard.Require(await db.Drivers.AnyAsync(d => d.DriverId == did && d.OwnerId == o.OwnerId), "That driver is not linked to you.");
            v.CurrentDriverId = r.DriverId; v.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- drivers
        g.MapGet("/drivers", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            return await db.Drivers.Where(d => d.OwnerId == o.OwnerId).OrderBy(d => d.User.FullName)
                .Select(d => new { id = d.DriverId, name = d.User.FullName, mobile = d.User.Mobile, d.LicenceNumber, d.LicenceClass, d.LicenceExpiry, d.KycStatus, d.DutyStatus, d.Rating })
                .ToListAsync();
        });

        // ---- available loads: paid bookings with no truck yet, matching one of this owner's approved, free vehicles
        g.MapGet("/loads", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            if (o.KycStatus != "Approved") return Results.Ok(new { kycApproved = false, loads = Array.Empty<object>() });
            var vehicles = await db.Vehicles.Where(v => v.OwnerId == o.OwnerId && v.VerificationStatus == "Approved" && v.AvailabilityStatus == "Available")
                .Select(v => new { v.VehicleId, v.RegistrationNumber, v.VehicleTypeId, v.CapacityKg, v.CurrentDriverId }).ToListAsync();
            var typeIds = vehicles.Select(v => v.VehicleTypeId).Distinct().ToList();
            var declined = db.LoadOffers.Where(l => l.OwnerId == o.OwnerId && l.Status == "Declined").Select(l => l.BookingId);
            var bookings = await db.Bookings
                .Where(b => b.Status == "Confirmed" && typeIds.Contains(b.VehicleTypeId) && !declined.Contains(b.BookingId))
                .OrderBy(b => b.PickupDate)
                .Select(b => new
                {
                    b.BookingId, b.BookingNumber, b.PickupDate, b.PickupSlot, b.WeightKg, b.VehicleTypeId, goods = b.GoodsDescription,
                    from = b.PickupCity!.Name, fromAddress = b.PickupAddressText, to = b.DropCity!.Name, vehicleType = b.VehicleType.Name,
                    quote = b.Quotes.Where(q => q.Status == "Accepted").Select(q => new { q.DistanceKm, q.OwnerPayout }).FirstOrDefault()
                }).ToListAsync();
            var loads = bookings.Select(b => new
            {
                id = b.BookingId, b.BookingNumber, b.PickupDate, b.PickupSlot, b.WeightKg, b.goods, b.from, b.fromAddress, b.to, b.vehicleType,
                distanceKm = b.quote?.DistanceKm, payout = b.quote?.OwnerPayout,
                vehicles = vehicles.Where(v => v.VehicleTypeId == b.VehicleTypeId && v.CapacityKg >= b.WeightKg)
                    .Select(v => new { id = v.VehicleId, v.RegistrationNumber, v.CurrentDriverId })
            }).Where(l => l.vehicles.Any()).ToList();
            return Results.Ok(new { kycApproved = true, loads });
        });

        g.MapPost("/loads/{bookingId:long}/accept", async (long bookingId, AcceptLoadRequest r, ClaimsPrincipal me, OlekarDbContext db, PiiProtector pii, HttpContext http) =>
        {
            var o = await CurrentOwner(db, me);
            Guard.Require(o.KycStatus == "Approved", "Your KYC must be approved before you can take loads.");
            var trip = await TripOps.AssignAsync(db, pii, bookingId, r.VehicleId, r.DriverId, o.OwnerId, me.UserId());
            db.LoadOffers.Add(new LoadOffer { BookingId = bookingId, OwnerId = o.OwnerId, VehicleId = r.VehicleId, OfferedPayout = trip.OwnerPayout, Status = "Accepted", ExpiresAt = DateTime.UtcNow, RespondedAt = DateTime.UtcNow });
            Audit.Log(db, http, "Load.Accepted", "Booking", bookingId, new { r.VehicleId, r.DriverId });
            await db.SaveChangesAsync();
            return Results.Ok(new { tripId = trip.TripId, trip.TripNumber });
        });

        g.MapPost("/loads/{bookingId:long}/decline", async (long bookingId, ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            db.LoadOffers.Add(new LoadOffer { BookingId = bookingId, OwnerId = o.OwnerId, OfferedPayout = 0, Status = "Declined", ExpiresAt = DateTime.UtcNow, RespondedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---- trips and money
        g.MapGet("/trips", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            return await db.Trips.Where(t => t.OwnerId == o.OwnerId).OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    id = t.TripId, t.TripNumber, t.Status, bookingNumber = t.Booking.BookingNumber,
                    from = t.Booking.PickupCity!.Name, to = t.Booking.DropCity!.Name, t.Booking.PickupDate,
                    vehicle = t.Vehicle.RegistrationNumber, driver = t.Driver.User.FullName, t.OwnerPayout, t.CreatedAt, t.DeliveredAt
                }).ToListAsync();
        });

        g.MapGet("/settlements", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var o = await CurrentOwner(db, me);
            return await db.Settlements.Where(s => s.OwnerId == o.OwnerId).OrderByDescending(s => s.CreatedAt)
                .Select(s => new { id = s.SettlementId, trip = s.Trip.TripNumber, s.GrossAmount, s.CommissionAmount, s.TdsAmount, s.NetAmount, s.Status, s.UTR, s.ReleasedAt, s.CreatedAt })
                .ToListAsync();
        });
    }

    public static async Task<Owner> CurrentOwner(OlekarDbContext db, ClaimsPrincipal me)
    {
        var uid = me.UserId();
        return await db.Owners.FirstOrDefaultAsync(o => o.UserId == uid) ?? throw ApiException.Forbidden("Owner profile not found.");
    }
}

public static class TripOps
{
    /// <summary>Creates a trip for a paid booking. Used by the owner "accept load" action and by admin manual assignment.</summary>
    public static async Task<Trip> AssignAsync(OlekarDbContext db, PiiProtector pii, long bookingId, long vehicleId, long driverId, long? mustBeOwnerId, long actorUserId)
    {
        var b = await db.Bookings.FirstOrDefaultAsync(x => x.BookingId == bookingId) ?? throw ApiException.NotFound("Booking not found.");
        if (b.Status != "Confirmed") throw ApiException.Conflict("This load has already been taken or is not paid yet.");
        var v = await db.Vehicles.FirstOrDefaultAsync(x => x.VehicleId == vehicleId) ?? throw ApiException.NotFound("Vehicle not found.");
        if (mustBeOwnerId is long own && v.OwnerId != own) throw ApiException.Forbidden("That vehicle is not yours.");
        Guard.Require(v.VerificationStatus == "Approved", "This vehicle is not verified yet.");
        Guard.Require(v.AvailabilityStatus == "Available", "This vehicle is not available.");
        Guard.Require(v.VehicleTypeId == b.VehicleTypeId, "The booking needs a different vehicle type.");
        Guard.Require(v.CapacityKg >= b.WeightKg, "The load is heavier than this vehicle's capacity.");
        var ownerKyc = await db.Owners.Where(o => o.OwnerId == v.OwnerId).Select(o => o.KycStatus).FirstAsync();
        Guard.Require(ownerKyc == "Approved", "The vehicle owner's KYC is not approved yet.");
        var d = await db.Drivers.FirstOrDefaultAsync(x => x.DriverId == driverId) ?? throw ApiException.NotFound("Driver not found.");
        Guard.Require(d.OwnerId == v.OwnerId, "The driver must be linked to the vehicle's owner.");
        Guard.Require(d.KycStatus == "Approved", "This driver is not verified yet.");
        Guard.Require(d.LicenceExpiry > DateOnly.FromDateTime(DateTime.UtcNow), "This driver's licence has expired.");
        Guard.Require(!await db.Trips.AnyAsync(t => t.DriverId == driverId && t.Status != "Completed" && t.Status != "Cancelled" && t.Status != "Delivered"), "This driver is already on a trip.");
        var quote = await db.Quotes.Where(q => q.BookingId == bookingId && q.Status == "Accepted").FirstAsync();

        var trip = new Trip
        {
            BookingId = bookingId, OwnerId = v.OwnerId, VehicleId = vehicleId, DriverId = driverId, Status = "Assigned",
            PickupOtpProtected = pii.Protect(OtpService.NewCode(4)), DeliveryOtpProtected = pii.Protect(OtpService.NewCode(4)),
            PlannedDistanceKm = quote.DistanceKm, OwnerPayout = quote.OwnerPayout
        };
        trip.Events.Add(new TripEvent { EventType = "Assigned", Note = $"Vehicle {v.RegistrationNumber} assigned", CreatedBy = actorUserId });
        db.Trips.Add(trip);
        b.Status = "Assigned"; b.UpdatedAt = DateTime.UtcNow;       // RowVersion stops two owners taking the same load
        v.AvailabilityStatus = "Busy"; v.CurrentDriverId = driverId; v.UpdatedAt = DateTime.UtcNow;
        d.DutyStatus = "OnTrip";
        await db.SaveChangesAsync();
        return trip;
    }
}
