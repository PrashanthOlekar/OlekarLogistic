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
/// Bookings and trips from the operations side.
///   GET  /api/admin/bookings?status=Confirmed        all bookings
///   GET  /api/admin/bookings/{id}/assignable         trucks and drivers that could take a booking
///   POST /api/admin/bookings/{id}/assign             assign a truck by hand
///   GET  /api/admin/trips?status=Delivered           all trips
///   POST /api/admin/trips/{id}/approve-pod           approve delivery proof → invoice + payout approved
/// </summary>
public static class AdminBookingEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/admin").RequireAuthorization(Roles.Admin);

        group.MapGet("/bookings", ListBookingsAsync);
        group.MapGet("/bookings/{id:long}/assignable", ListAssignableTrucksAsync);
        group.MapPost("/bookings/{id:long}/assign", AssignTruckAsync);

        group.MapGet("/trips", ListTripsAsync);
        group.MapPost("/trips/{id:long}/approve-pod", ApprovePodAsync);
    }

    // ---------------------------------------------------------------- bookings

    private static async Task<IResult> ListBookingsAsync(string? status, ProCargoDbContext db)
    {
        var bookings = await db.Bookings
            .Where(booking => status == null || booking.Status == status)
            .OrderByDescending(booking => booking.CreatedAt)
            .Take(200)
            .Select(booking => new
            {
                id = booking.BookingId,
                booking.BookingNumber,
                booking.Status,
                customer = booking.Customer.CompanyName ?? booking.Customer.User.FullName,
                customerMobile = booking.Customer.User.Mobile,
                from = booking.PickupCity!.Name,
                to = booking.DropCity!.Name,
                booking.PickupDate,
                booking.WeightKg,
                vehicleType = booking.VehicleType.Name,
                booking.VehicleTypeId,
                booking.CreatedAt,
                total = booking.Quotes
                    .OrderByDescending(quote => quote.CreatedAt)
                    .Select(quote => (decimal?)quote.TotalAmount)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        return Results.Ok(bookings);
    }

    /// <summary>Verified, free trucks of the right type and size, each with the owner's free verified drivers.</summary>
    private static async Task<IResult> ListAssignableTrucksAsync(long id, ProCargoDbContext db)
    {
        Booking booking = await db.Bookings.FindAsync(id)
            ?? throw ApiException.NotFound("Booking not found.");

        var trucks = await db.Vehicles
            .Where(vehicle => vehicle.VehicleTypeId == booking.VehicleTypeId
                && vehicle.CapacityKg >= booking.WeightKg
                && vehicle.VerificationStatus == VehicleVerification.Approved
                && vehicle.AvailabilityStatus == VehicleAvailability.Available
                && vehicle.Owner.KycStatus == KycStatus.Approved)
            .Select(vehicle => new
            {
                id = vehicle.VehicleId,
                vehicle.RegistrationNumber,
                owner = vehicle.Owner.User.FullName,
                vehicle.CurrentDriverId,
                drivers = vehicle.Owner.Drivers
                    .Where(driver => driver.KycStatus == KycStatus.Approved && driver.DutyStatus == DutyStatus.Available)
                    .Select(driver => new { id = driver.DriverId, name = driver.User.FullName })
                    .ToList(),
            })
            .ToListAsync();

        return Results.Ok(trucks);
    }

    private static async Task<IResult> AssignTruckAsync(
        long id,
        AssignTruckRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        TripService tripService,
        HttpContext http)
    {
        Trip trip = await tripService.AssignAsync(
            id,
            request.VehicleId,
            request.DriverId,
            requiredOwnerId: null,
            actorUserId: principal.GetUserId());

        AuditLogger.Log(db, http, "Booking.AssignedByAdmin", "Booking", id, new { request.VehicleId, request.DriverId });
        await db.SaveChangesAsync();

        return Results.Ok(new { tripId = trip.TripId, trip.TripNumber });
    }

    // ---------------------------------------------------------------- trips

    private static async Task<IResult> ListTripsAsync(string? status, ProCargoDbContext db)
    {
        var trips = await db.Trips
            .Where(trip => status == null || trip.Status == status)
            .OrderByDescending(trip => trip.CreatedAt)
            .Take(200)
            .Select(trip => new
            {
                id = trip.TripId,
                trip.TripNumber,
                trip.Status,
                bookingNumber = trip.Booking.BookingNumber,
                from = trip.Booking.PickupCity!.Name,
                to = trip.Booking.DropCity!.Name,
                vehicle = trip.Vehicle.RegistrationNumber,
                driver = trip.Driver.User.FullName,
                driverMobile = trip.Driver.User.Mobile,
                owner = trip.Owner.User.FullName,
                trip.CreatedAt,
                trip.DeliveredAt,
                pod = db.Documents
                    .Where(document => document.EntityType == DocumentEntity.Trip
                        && document.EntityId == trip.TripId
                        && document.DocType == DocumentType.Pod)
                    .OrderByDescending(document => document.UploadedAt)
                    .Select(document => (long?)document.DocumentId)
                    .FirstOrDefault(),
                lastEvent = trip.Events
                    .OrderByDescending(tripEvent => tripEvent.CreatedAt)
                    .Select(tripEvent => tripEvent.EventType)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        return Results.Ok(trips);
    }

    /// <summary>
    /// The admin has checked the signed delivery receipt. This:
    ///   1. completes the trip and the booking,
    ///   2. approves the owner's payout (it can now be released),
    ///   3. marks the POD documents as verified,
    ///   4. issues the customer's GST invoice.
    /// </summary>
    private static async Task<IResult> ApprovePodAsync(
        long id,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        InvoiceService invoiceService,
        HttpContext http)
    {
        Trip trip = await db.Trips
            .Include(row => row.Booking).ThenInclude(booking => booking.PickupCity)
            .Include(row => row.Booking).ThenInclude(booking => booking.DropCity)
            .Include(row => row.Booking).ThenInclude(booking => booking.Customer)
            .FirstOrDefaultAsync(row => row.TripId == id)
            ?? throw ApiException.NotFound("Trip not found.");

        Guard.Require(trip.Status == TripStatus.Delivered, "Only delivered trips can have their POD approved.");

        long adminUserId = principal.GetUserId();
        DateTime now = DateTime.UtcNow;

        // 1. Trip and booking
        trip.Status = TripStatus.Completed;
        trip.PodApprovedBy = adminUserId;
        trip.PodApprovedAt = now;
        trip.UpdatedAt = now;
        trip.Booking.Status = BookingStatus.Completed;
        trip.Booking.UpdatedAt = now;

        db.TripEvents.Add(new TripEvent
        {
            TripId = id,
            EventType = TripEventType.PodApproved,
            CreatedBy = adminUserId,
        });

        // 2. Owner payout
        Settlement? settlement = await db.Settlements.FirstOrDefaultAsync(row => row.TripId == id);
        if (settlement is not null)
        {
            settlement.Status = SettlementStatus.Approved;
            settlement.ApprovedBy = adminUserId;
        }

        // 3. POD documents
        List<Document> podDocuments = await db.Documents
            .Where(document => document.EntityType == DocumentEntity.Trip
                && document.EntityId == id
                && document.DocType == DocumentType.Pod)
            .ToListAsync();
        foreach (Document document in podDocuments)
        {
            document.Status = DocumentStatus.Verified;
            document.ReviewedBy = adminUserId;
            document.ReviewedAt = now;
        }

        // 4. Customer invoice
        await invoiceService.CreateForBookingAsync(trip.Booking);

        AuditLogger.Log(db, http, "Trip.PodApproved", "Trip", id);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}
