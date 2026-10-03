using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Contracts;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;
using ProCargo.Api.Services;

namespace ProCargo.Api.Endpoints;

/// <summary>
/// The driver app. Each driver action moves the trip exactly one step:
///
///   Assigned → EnRouteToPickup → AtPickup ─(pickup code)→ Loaded → InTransit
///            → AtDestination ─(POD photo + delivery code)→ Delivered
///
///   GET  /api/driver/trips                  my trips, current ones first
///   GET  /api/driver/trips/{id}             one trip with addresses and timeline
///   POST /api/driver/trips/{id}/advance     EnRoute | ReachedPickup | StartTrip | ReachedDestination
///   POST /api/driver/trips/{id}/verify-otp  Pickup or Delivery code
///   POST /api/driver/trips/{id}/photo       goods photo or signed POD (multipart form)
/// </summary>
public static class DriverEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/driver").RequireAuthorization(Roles.Driver);

        group.MapGet("/trips", ListTripsAsync);
        group.MapGet("/trips/{id:long}", GetTripAsync);
        group.MapPost("/trips/{id:long}/advance", AdvanceTripAsync);
        group.MapPost("/trips/{id:long}/verify-otp", VerifyTripCodeAsync);
        group.MapPost("/trips/{id:long}/photo", UploadTripPhotoAsync).DisableAntiforgery();
    }

    /// <summary>One driver action: the statuses it is allowed from, the status it moves to, and the event it records.</summary>
    private sealed record TripStep(string[] AllowedFrom, string NextStatus, string EventType);

    private static readonly Dictionary<string, TripStep> Steps = new()
    {
        ["EnRoute"] = new(
            new[] { TripStatus.Assigned },
            TripStatus.EnRouteToPickup,
            TripEventType.EnRouteToPickup),

        ["ReachedPickup"] = new(
            new[] { TripStatus.Assigned, TripStatus.EnRouteToPickup },
            TripStatus.AtPickup,
            TripEventType.ReachedPickup),

        ["StartTrip"] = new(
            new[] { TripStatus.Loaded },
            TripStatus.InTransit,
            TripEventType.TripStarted),

        ["ReachedDestination"] = new(
            new[] { TripStatus.InTransit },
            TripStatus.AtDestination,
            TripEventType.ReachedDestination),
    };

    // ---------------------------------------------------------------- read

    private static async Task<IResult> ListTripsAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Driver driver = await GetCurrentDriverAsync(db, principal);

        var trips = await db.Trips
            .Where(trip => trip.DriverId == driver.DriverId)
            .OrderBy(trip => trip.Status == TripStatus.Completed || trip.Status == TripStatus.Cancelled)
            .ThenByDescending(trip => trip.CreatedAt)
            .Select(trip => new
            {
                id = trip.TripId,
                trip.TripNumber,
                trip.Status,
                from = trip.Booking.PickupCity!.Name,
                to = trip.Booking.DropCity!.Name,
                trip.Booking.PickupDate,
                vehicle = trip.Vehicle.RegistrationNumber,
            })
            .ToListAsync();

        return Results.Ok(trips);
    }

    private static async Task<IResult> GetTripAsync(long id, ClaimsPrincipal principal, ProCargoDbContext db)
    {
        Driver driver = await GetCurrentDriverAsync(db, principal);

        Trip trip = await db.Trips
            .Include(row => row.Booking).ThenInclude(booking => booking.PickupCity)
            .Include(row => row.Booking).ThenInclude(booking => booking.DropCity)
            .Include(row => row.Vehicle)
            .Include(row => row.Events)
            .FirstOrDefaultAsync(row => row.TripId == id && row.DriverId == driver.DriverId)
            ?? throw ApiException.NotFound("Trip not found.");

        bool hasPod = await HasPodAsync(db, id);
        Booking booking = trip.Booking;

        return Results.Ok(new
        {
            id = trip.TripId,
            trip.TripNumber,
            trip.Status,
            vehicle = trip.Vehicle.RegistrationNumber,
            hasPod,
            booking = new
            {
                booking.BookingNumber,
                booking.GoodsDescription,
                booking.WeightKg,
                booking.PickupDate,
                booking.PickupSlot,
                booking.SpecialInstructions,
                pickup = new
                {
                    city = booking.PickupCity?.Name,
                    address = booking.PickupAddressText,
                    contact = booking.PickupContactName,
                    phone = booking.PickupContactPhone,
                },
                drop = new
                {
                    city = booking.DropCity?.Name,
                    address = booking.DropAddressText,
                    contact = booking.DropContactName,
                    phone = booking.DropContactPhone,
                },
            },
            trip.PlannedDistanceKm,
            trip.DriverPay,
            events = trip.Events
                .OrderBy(tripEvent => tripEvent.CreatedAt)
                .Select(tripEvent => new { tripEvent.EventType, tripEvent.Note, tripEvent.CreatedAt }),
        });
    }

    // ---------------------------------------------------------------- trip steps

    private static async Task<IResult> AdvanceTripAsync(
        long id,
        AdvanceTripRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db)
    {
        Trip trip = await GetOwnTripAsync(db, principal, id);

        if (!Steps.TryGetValue(request.Action ?? string.Empty, out TripStep? step))
        {
            throw ApiException.BadRequest("Unknown action.");
        }
        if (!step.AllowedFrom.Contains(trip.Status))
        {
            throw ApiException.Conflict($"This step isn't available while the trip is {trip.Status}.");
        }

        DateTime now = DateTime.UtcNow;
        trip.Status = step.NextStatus;
        trip.UpdatedAt = now;

        switch (step.NextStatus)
        {
            case TripStatus.AtPickup:
                trip.ReachedPickupAt = now;
                break;

            case TripStatus.InTransit:
                trip.StartedAt = now;
                Booking booking = await db.Bookings.FirstAsync(row => row.BookingId == trip.BookingId);
                booking.Status = BookingStatus.InTransit;
                booking.UpdatedAt = now;
                break;

            case TripStatus.AtDestination:
                trip.ReachedDropAt = now;
                break;
        }

        db.TripEvents.Add(new TripEvent
        {
            TripId = trip.TripId,
            EventType = step.EventType,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedBy = principal.GetUserId(),
        });

        if (request.Latitude is decimal latitude && request.Longitude is decimal longitude)
        {
            Vehicle vehicle = await db.Vehicles.FirstAsync(row => row.VehicleId == trip.VehicleId);
            vehicle.LastLatitude = latitude;
            vehicle.LastLongitude = longitude;
            vehicle.LastSeenAt = now;
        }

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    /// <summary>
    /// The sender reads out the pickup code; the receiver reads out the delivery code.
    /// The customer sees both codes in their booking.
    /// </summary>
    private static async Task<IResult> VerifyTripCodeAsync(
        long id,
        VerifyTripCodeRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        PersonalDataProtector protector,
        TripService tripService)
    {
        Trip trip = await GetOwnTripAsync(db, principal, id);
        string code = (request.Code ?? string.Empty).Trim();
        long userId = principal.GetUserId();

        switch (request.Kind)
        {
            case "Pickup":
                if (trip.Status != TripStatus.AtPickup)
                {
                    throw ApiException.Conflict("Mark \"Reached pickup\" first.");
                }
                Guard.Require(code == protector.Unprotect(trip.PickupOtpProtected!), "That code doesn't match. Ask the sender to check it.");

                trip.Status = TripStatus.Loaded;
                trip.LoadedAt = DateTime.UtcNow;
                db.TripEvents.Add(new TripEvent
                {
                    TripId = trip.TripId,
                    EventType = TripEventType.PickupOtpVerified,
                    Note = "Goods handed over",
                    CreatedBy = userId,
                });
                break;

            case "Delivery":
                if (trip.Status != TripStatus.AtDestination)
                {
                    throw ApiException.Conflict("Mark \"Reached destination\" first.");
                }
                Guard.Require(await HasPodAsync(db, id), "Upload the signed delivery receipt (POD) first.");
                Guard.Require(code == protector.Unprotect(trip.DeliveryOtpProtected!), "That code doesn't match. Ask the receiver to check it.");

                await tripService.CompleteDeliveryAsync(trip, userId);
                break;

            default:
                throw ApiException.BadRequest("Kind must be Pickup or Delivery.");
        }

        trip.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    /// <summary>Form fields: kind (PickupPhoto or POD), file, latitude, longitude.</summary>
    private static async Task<IResult> UploadTripPhotoAsync(
        long id,
        HttpRequest httpRequest,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        FileStorage storage)
    {
        Trip trip = await GetOwnTripAsync(db, principal, id);
        IFormCollection form = await httpRequest.ReadFormAsync();
        string kind = form["kind"].ToString();

        Guard.Require(kind is DocumentType.PickupPhoto or DocumentType.Pod, "Kind must be PickupPhoto or POD.");
        if (kind == DocumentType.Pod)
        {
            Guard.Require(trip.Status == TripStatus.AtDestination, "Upload the POD after you reach the destination.");
        }
        else
        {
            Guard.Require(trip.Status is TripStatus.AtPickup or TripStatus.Loaded, "Upload the goods photo at pickup.");
        }

        IFormFile file = form.Files.FirstOrDefault() ?? throw ApiException.BadRequest("Attach a photo.");
        StoredFile saved = await storage.SaveAsync(file);
        decimal? latitude = ParseDecimal(form["latitude"]);
        decimal? longitude = ParseDecimal(form["longitude"]);
        long userId = principal.GetUserId();

        var document = new Document
        {
            EntityType = DocumentEntity.Trip,
            EntityId = id,
            DocType = kind,
            BlobPath = saved.Path,
            FileName = saved.FileName,
            ContentType = saved.ContentType,
            SizeBytes = saved.SizeBytes,
            Sha256 = saved.Sha256,
            UploadedBy = userId,
            Latitude = latitude,
            Longitude = longitude,
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        db.TripEvents.Add(new TripEvent
        {
            TripId = id,
            EventType = kind == DocumentType.Pod ? TripEventType.PodUploaded : TripEventType.GoodsPhotoUploaded,
            DocumentId = document.DocumentId,
            Latitude = latitude,
            Longitude = longitude,
            CreatedBy = userId,
        });
        await db.SaveChangesAsync();

        return Results.Ok(new { documentId = document.DocumentId });
    }

    // ---------------------------------------------------------------- helpers

    private static async Task<Driver> GetCurrentDriverAsync(ProCargoDbContext db, ClaimsPrincipal principal)
    {
        long userId = principal.GetUserId();

        return await db.Drivers.FirstOrDefaultAsync(driver => driver.UserId == userId)
            ?? throw ApiException.Forbidden("Driver profile not found.");
    }

    private static async Task<Trip> GetOwnTripAsync(ProCargoDbContext db, ClaimsPrincipal principal, long tripId)
    {
        Driver driver = await GetCurrentDriverAsync(db, principal);

        return await db.Trips.FirstOrDefaultAsync(trip => trip.TripId == tripId && trip.DriverId == driver.DriverId)
            ?? throw ApiException.NotFound("Trip not found.");
    }

    private static Task<bool> HasPodAsync(ProCargoDbContext db, long tripId)
    {
        return db.Documents.AnyAsync(document =>
            document.EntityType == DocumentEntity.Trip
            && document.EntityId == tripId
            && document.DocType == DocumentType.Pod);
    }

    private static decimal? ParseDecimal(string? text)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            ? value
            : null;
    }
}
