using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Contracts;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;
using ProCargo.Api.Services;

namespace ProCargo.Api.Endpoints;

/// <summary>
/// Lorry owners. Owners only ever see their own vehicles, drivers, trips and payouts.
///   GET   /api/owner/summary                      totals for the overview page
///   GET   /api/owner/vehicles                     my vehicles and their documents
///   POST  /api/owner/vehicles                     add a vehicle
///   PATCH /api/owner/vehicles/{id}/availability   Available / Busy / Maintenance
///   PATCH /api/owner/vehicles/{id}/driver         set the regular driver
///   GET   /api/owner/drivers                      drivers linked to me
///   GET   /api/owner/loads                        paid loads my free trucks can take
///   POST  /api/owner/loads/{bookingId}/accept     take a load
///   POST  /api/owner/loads/{bookingId}/decline    hide a load
///   GET   /api/owner/trips                        my trips
///   GET   /api/owner/settlements                  my payouts
/// </summary>
public static class OwnerEndpoints
{
    // Indian registration numbers, e.g. KA01AB4521, MH12K1234
    private static readonly Regex RegistrationFormat = new("^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{4}$");

    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/owner").RequireAuthorization(Roles.Owner);

        group.MapGet("/summary", GetSummaryAsync);

        group.MapGet("/vehicles", ListVehiclesAsync);
        group.MapPost("/vehicles", AddVehicleAsync);
        group.MapPatch("/vehicles/{id:long}/availability", SetAvailabilityAsync);
        group.MapPatch("/vehicles/{id:long}/driver", SetRegularDriverAsync);

        group.MapGet("/drivers", ListDriversAsync);

        group.MapGet("/loads", ListAvailableLoadsAsync);
        group.MapPost("/loads/{bookingId:long}/accept", AcceptLoadAsync);
        group.MapPost("/loads/{bookingId:long}/decline", DeclineLoadAsync);

        group.MapGet("/trips", ListTripsAsync);
        group.MapGet("/settlements", ListSettlementsAsync);
    }

    // ---------------------------------------------------------------- overview

    private static async Task<IResult> GetSummaryAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);
        long ownerId = owner.OwnerId;
        DateTime monthStart = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var bank = await db.OwnerBankAccounts
            .Where(account => account.OwnerId == ownerId && account.IsPrimary && account.IsActive)
            .Select(account => new { account.AccountHolder, account.AccountLast4, account.IFSC, account.BankName, account.PennyDropStatus })
            .FirstOrDefaultAsync();

        decimal earnedThisMonth = await db.Settlements
            .Where(settlement => settlement.OwnerId == ownerId
                && settlement.Status == SettlementStatus.Released
                && settlement.ReleasedAt >= monthStart)
            .SumAsync(settlement => (decimal?)settlement.NetAmount) ?? 0;

        decimal pendingPayout = await db.Settlements
            .Where(settlement => settlement.OwnerId == ownerId && settlement.Status != SettlementStatus.Released)
            .SumAsync(settlement => (decimal?)settlement.NetAmount) ?? 0;

        return Results.Ok(new
        {
            owner.OwnerId,
            owner.BusinessName,
            owner.KycStatus,
            owner.RejectionReason,
            vehicles = await db.Vehicles.CountAsync(vehicle => vehicle.OwnerId == ownerId),
            vehiclesApproved = await db.Vehicles.CountAsync(vehicle =>
                vehicle.OwnerId == ownerId && vehicle.VerificationStatus == VehicleVerification.Approved),
            drivers = await db.Drivers.CountAsync(driver => driver.OwnerId == ownerId),
            activeTrips = await db.Trips.CountAsync(trip =>
                trip.OwnerId == ownerId && trip.Status != TripStatus.Completed && trip.Status != TripStatus.Cancelled),
            earnedThisMonth,
            pendingPayout,
            bank,
        });
    }

    // ---------------------------------------------------------------- vehicles

    private static async Task<IResult> ListVehiclesAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);

        var vehicles = await db.Vehicles
            .Where(vehicle => vehicle.OwnerId == owner.OwnerId)
            .OrderBy(vehicle => vehicle.RegistrationNumber)
            .Select(vehicle => new
            {
                id = vehicle.VehicleId,
                vehicle.RegistrationNumber,
                vehicleType = vehicle.VehicleType.Name,
                vehicle.VehicleTypeId,
                vehicle.CapacityKg,
                vehicle.MakeModel,
                vehicle.AvailabilityStatus,
                vehicle.VerificationStatus,
                vehicle.CurrentDriverId,
                driverName = vehicle.CurrentDriver != null ? vehicle.CurrentDriver.User.FullName : null,
                documents = db.Documents
                    .Where(document => document.EntityType == DocumentEntity.Vehicle && document.EntityId == vehicle.VehicleId)
                    .Select(document => new { id = document.DocumentId, document.DocType, document.Status, document.ExpiryDate })
                    .ToList(),
            })
            .ToListAsync();

        return Results.Ok(vehicles);
    }

    private static async Task<IResult> AddVehicleAsync(
        AddVehicleRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        HttpContext http)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);

        // "KA 01 AB 4521" → "KA01AB4521"
        string registration = new string((request.RegistrationNumber ?? string.Empty).Where(char.IsLetterOrDigit).ToArray())
            .ToUpperInvariant();

        Guard.Require(RegistrationFormat.IsMatch(registration), "Enter the registration number as on the RC, for example KA01AB4521.");
        Guard.Require(!await db.Vehicles.AnyAsync(vehicle => vehicle.RegistrationNumber == registration), "This vehicle is already registered.");
        Guard.Require(request.CapacityKg is > 0 and <= 60000, "Enter the vehicle's load capacity in kg.");

        VehicleType vehicleType = await db.VehicleTypes.FindAsync(request.VehicleTypeId)
            ?? throw ApiException.BadRequest("Choose the vehicle type.");

        var vehicle = new Vehicle
        {
            OwnerId = owner.OwnerId,
            VehicleTypeId = vehicleType.VehicleTypeId,
            RegistrationNumber = registration,
            CapacityKg = request.CapacityKg,
            MakeModel = request.MakeModel,
            ManufactureYear = request.ManufactureYear,
            HomeCityId = request.HomeCityId,
            AvailabilityStatus = VehicleAvailability.Available,
            VerificationStatus = VehicleVerification.Pending,
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        AuditLogger.Log(db, http, "Vehicle.Added", "Vehicle", vehicle.VehicleId, new { registration });
        await db.SaveChangesAsync();

        return Results.Created($"/api/owner/vehicles/{vehicle.VehicleId}", new { id = vehicle.VehicleId });
    }

    private static async Task<IResult> SetAvailabilityAsync(
        long id,
        SetAvailabilityRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);
        Guard.Require(
            request.Status is VehicleAvailability.Available or VehicleAvailability.Busy or VehicleAvailability.Maintenance,
            "Choose Available, Busy or Maintenance.");

        Vehicle vehicle = await db.Vehicles.FirstOrDefaultAsync(row => row.VehicleId == id && row.OwnerId == owner.OwnerId)
            ?? throw ApiException.NotFound("Vehicle not found.");

        bool isOnTrip = await db.Trips.AnyAsync(trip => trip.VehicleId == id && !TripStatus.Finished.Contains(trip.Status));
        Guard.Require(
            !isOnTrip || request.Status == VehicleAvailability.Busy,
            "This vehicle is on a trip. It becomes available again after delivery.");

        vehicle.AvailabilityStatus = request.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> SetRegularDriverAsync(
        long id,
        SetRegularDriverRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);

        Vehicle vehicle = await db.Vehicles.FirstOrDefaultAsync(row => row.VehicleId == id && row.OwnerId == owner.OwnerId)
            ?? throw ApiException.NotFound("Vehicle not found.");

        if (request.DriverId is long driverId)
        {
            bool isMyDriver = await db.Drivers.AnyAsync(driver => driver.DriverId == driverId && driver.OwnerId == owner.OwnerId);
            Guard.Require(isMyDriver, "That driver is not linked to you.");
        }

        vehicle.CurrentDriverId = request.DriverId;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // ---------------------------------------------------------------- drivers

    private static async Task<IResult> ListDriversAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);

        var drivers = await db.Drivers
            .Where(driver => driver.OwnerId == owner.OwnerId)
            .OrderBy(driver => driver.User.FullName)
            .Select(driver => new
            {
                id = driver.DriverId,
                name = driver.User.FullName,
                mobile = driver.User.Mobile,
                driver.LicenceNumber,
                driver.LicenceClass,
                driver.LicenceExpiry,
                driver.KycStatus,
                driver.DutyStatus,
                driver.Rating,
            })
            .ToListAsync();

        return Results.Ok(drivers);
    }

    // ---------------------------------------------------------------- loads

    /// <summary>
    /// Paid bookings that still need a truck and that one of my verified, available vehicles can carry.
    /// Loads I declined are hidden.
    /// </summary>
    private static async Task<IResult> ListAvailableLoadsAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);
        if (owner.KycStatus != KycStatus.Approved)
        {
            return Results.Ok(new { kycApproved = false, loads = Array.Empty<object>() });
        }

        var freeVehicles = await db.Vehicles
            .Where(vehicle => vehicle.OwnerId == owner.OwnerId
                && vehicle.VerificationStatus == VehicleVerification.Approved
                && vehicle.AvailabilityStatus == VehicleAvailability.Available)
            .Select(vehicle => new { vehicle.VehicleId, vehicle.RegistrationNumber, vehicle.VehicleTypeId, vehicle.CapacityKg, vehicle.CurrentDriverId })
            .ToListAsync();

        List<int> vehicleTypeIds = freeVehicles.Select(vehicle => vehicle.VehicleTypeId).Distinct().ToList();

        IQueryable<long> declinedBookingIds = db.LoadOffers
            .Where(offer => offer.OwnerId == owner.OwnerId && offer.Status == LoadOfferStatus.Declined)
            .Select(offer => offer.BookingId);

        var paidBookings = await db.Bookings
            .Where(booking => booking.Status == BookingStatus.Confirmed
                && vehicleTypeIds.Contains(booking.VehicleTypeId)
                && !declinedBookingIds.Contains(booking.BookingId))
            .OrderBy(booking => booking.PickupDate)
            .Select(booking => new
            {
                booking.BookingId,
                booking.BookingNumber,
                booking.PickupDate,
                booking.PickupSlot,
                booking.WeightKg,
                booking.VehicleTypeId,
                goods = booking.GoodsDescription,
                from = booking.PickupCity!.Name,
                fromAddress = booking.PickupAddressText,
                to = booking.DropCity!.Name,
                vehicleType = booking.VehicleType.Name,
                quote = booking.Quotes
                    .Where(quote => quote.Status == QuoteStatus.Accepted)
                    .Select(quote => new { quote.DistanceKm, quote.OwnerPayout })
                    .FirstOrDefault(),
            })
            .ToListAsync();

        // Pair each load with my vehicles that can carry it; keep loads with at least one match.
        var loads = paidBookings
            .Select(booking => new
            {
                id = booking.BookingId,
                booking.BookingNumber,
                booking.PickupDate,
                booking.PickupSlot,
                booking.WeightKg,
                booking.goods,
                booking.from,
                booking.fromAddress,
                booking.to,
                booking.vehicleType,
                distanceKm = booking.quote?.DistanceKm,
                payout = booking.quote?.OwnerPayout,
                vehicles = freeVehicles
                    .Where(vehicle => vehicle.VehicleTypeId == booking.VehicleTypeId && vehicle.CapacityKg >= booking.WeightKg)
                    .Select(vehicle => new { id = vehicle.VehicleId, vehicle.RegistrationNumber, vehicle.CurrentDriverId })
                    .ToList(),
            })
            .Where(load => load.vehicles.Count > 0)
            .ToList();

        return Results.Ok(new { kycApproved = true, loads });
    }

    private static async Task<IResult> AcceptLoadAsync(
        long bookingId,
        AcceptLoadRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        TripService trips,
        HttpContext http)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);
        Guard.Require(owner.KycStatus == KycStatus.Approved, "Your KYC must be approved before you can take loads.");

        Trip trip = await trips.AssignAsync(bookingId, request.VehicleId, request.DriverId, owner.OwnerId, principal.GetUserId());

        db.LoadOffers.Add(new LoadOffer
        {
            BookingId = bookingId,
            OwnerId = owner.OwnerId,
            VehicleId = request.VehicleId,
            OfferedPayout = trip.OwnerPayout,
            Status = LoadOfferStatus.Accepted,
            ExpiresAt = DateTime.UtcNow,
            RespondedAt = DateTime.UtcNow,
        });
        AuditLogger.Log(db, http, "Load.Accepted", "Booking", bookingId, new { request.VehicleId, request.DriverId });
        await db.SaveChangesAsync();

        return Results.Ok(new { tripId = trip.TripId, trip.TripNumber });
    }

    private static async Task<IResult> DeclineLoadAsync(long bookingId, ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);

        db.LoadOffers.Add(new LoadOffer
        {
            BookingId = bookingId,
            OwnerId = owner.OwnerId,
            OfferedPayout = 0,
            Status = LoadOfferStatus.Declined,
            ExpiresAt = DateTime.UtcNow,
            RespondedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // ---------------------------------------------------------------- trips and payouts

    private static async Task<IResult> ListTripsAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);

        var trips = await db.Trips
            .Where(trip => trip.OwnerId == owner.OwnerId)
            .OrderByDescending(trip => trip.CreatedAt)
            .Select(trip => new
            {
                id = trip.TripId,
                trip.TripNumber,
                trip.Status,
                bookingNumber = trip.Booking.BookingNumber,
                from = trip.Booking.PickupCity!.Name,
                to = trip.Booking.DropCity!.Name,
                trip.Booking.PickupDate,
                vehicle = trip.Vehicle.RegistrationNumber,
                driver = trip.Driver.User.FullName,
                trip.OwnerPayout,
                trip.CreatedAt,
                trip.DeliveredAt,
            })
            .ToListAsync();

        return Results.Ok(trips);
    }

    private static async Task<IResult> ListSettlementsAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Owner owner = await GetCurrentOwnerAsync(db, principal);

        var settlements = await db.Settlements
            .Where(settlement => settlement.OwnerId == owner.OwnerId)
            .OrderByDescending(settlement => settlement.CreatedAt)
            .Select(settlement => new
            {
                id = settlement.SettlementId,
                trip = settlement.Trip.TripNumber,
                settlement.GrossAmount,
                settlement.CommissionAmount,
                settlement.TdsAmount,
                settlement.NetAmount,
                settlement.Status,
                settlement.UTR,
                settlement.ReleasedAt,
                settlement.CreatedAt,
            })
            .ToListAsync();

        return Results.Ok(settlements);
    }

    // ---------------------------------------------------------------- helpers

    private static async Task<Owner> GetCurrentOwnerAsync(ProCargoDbContext db, ClaimsPrincipal principal)
    {
        long userId = principal.GetUserId();

        return await db.Owners.FirstOrDefaultAsync(owner => owner.UserId == userId)
            ?? throw ApiException.Forbidden("Owner profile not found.");
    }
}
