using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;

namespace ProCargo.Api.Services;

/// <summary>
/// The two big moments in a trip's life:
///   1. AssignAsync         — a paid booking gets a truck and driver (creates the Trip).
///   2. CompleteDeliveryAsync — the receiver's code is confirmed (frees the truck, opens the owner's payout).
/// </summary>
public class TripService
{
    private readonly ProCargoDbContext _db;
    private readonly PersonalDataProtector _protector;

    public TripService(ProCargoDbContext db, PersonalDataProtector protector)
    {
        _db = db;
        _protector = protector;
    }

    /// <summary>
    /// Creates a trip for a paid booking.
    /// Used when an owner takes a load, and when an admin assigns a truck by hand.
    /// </summary>
    /// <param name="requiredOwnerId">When an owner takes the load, the vehicle must be theirs.</param>
    public async Task<Trip> AssignAsync(long bookingId, long vehicleId, long driverId, long? requiredOwnerId, long actorUserId)
    {
        Booking booking = await _db.Bookings.FirstOrDefaultAsync(row => row.BookingId == bookingId)
            ?? throw ApiException.NotFound("Booking not found.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw ApiException.Conflict("This load has already been taken or is not paid yet.");
        }

        Vehicle vehicle = await _db.Vehicles.FirstOrDefaultAsync(row => row.VehicleId == vehicleId)
            ?? throw ApiException.NotFound("Vehicle not found.");
        await CheckVehicleCanTakeBookingAsync(vehicle, booking, requiredOwnerId);

        Driver driver = await _db.Drivers.FirstOrDefaultAsync(row => row.DriverId == driverId)
            ?? throw ApiException.NotFound("Driver not found.");
        await CheckDriverCanDriveAsync(driver, vehicle);

        Quote acceptedQuote = await _db.Quotes
            .Where(quote => quote.BookingId == bookingId && quote.Status == QuoteStatus.Accepted)
            .FirstAsync();

        var trip = new Trip
        {
            BookingId = bookingId,
            OwnerId = vehicle.OwnerId,
            VehicleId = vehicleId,
            DriverId = driverId,
            Status = TripStatus.Assigned,
            PickupOtpProtected = _protector.Protect(OtpService.NewCode(4)),
            DeliveryOtpProtected = _protector.Protect(OtpService.NewCode(4)),
            PlannedDistanceKm = acceptedQuote.DistanceKm,
            OwnerPayout = acceptedQuote.OwnerPayout,
        };
        trip.Events.Add(new TripEvent
        {
            EventType = TripEventType.Assigned,
            Note = $"Vehicle {vehicle.RegistrationNumber} assigned",
            CreatedBy = actorUserId,
        });
        _db.Trips.Add(trip);

        // The booking's RowVersion stops two owners taking the same load at the same moment.
        booking.Status = BookingStatus.Assigned;
        booking.UpdatedAt = DateTime.UtcNow;

        vehicle.AvailabilityStatus = VehicleAvailability.Busy;
        vehicle.CurrentDriverId = driverId;
        vehicle.UpdatedAt = DateTime.UtcNow;

        driver.DutyStatus = DutyStatus.OnTrip;

        await _db.SaveChangesAsync();
        return trip;
    }

    /// <summary>
    /// The goods are handed over: frees the truck and driver, and creates the owner's
    /// settlement, which waits for an admin to approve the delivery proof (POD).
    /// The caller saves the changes.
    /// </summary>
    public async Task CompleteDeliveryAsync(Trip trip, long actorUserId)
    {
        trip.Status = TripStatus.Delivered;
        trip.DeliveredAt = DateTime.UtcNow;
        trip.UpdatedAt = DateTime.UtcNow;

        _db.TripEvents.Add(new TripEvent
        {
            TripId = trip.TripId,
            EventType = TripEventType.DeliveryOtpVerified,
            Note = "Goods delivered",
            CreatedBy = actorUserId,
        });

        Booking booking = await _db.Bookings.FirstAsync(row => row.BookingId == trip.BookingId);
        booking.Status = BookingStatus.Delivered;
        booking.UpdatedAt = DateTime.UtcNow;

        Vehicle vehicle = await _db.Vehicles.FirstAsync(row => row.VehicleId == trip.VehicleId);
        vehicle.AvailabilityStatus = VehicleAvailability.Available;

        Driver driver = await _db.Drivers.FirstAsync(row => row.DriverId == trip.DriverId);
        driver.DutyStatus = DutyStatus.Available;

        await CreateSettlementAsync(trip);
    }

    private async Task CreateSettlementAsync(Trip trip)
    {
        Quote quote = await _db.Quotes
            .Where(row => row.BookingId == trip.BookingId && row.Status == QuoteStatus.Accepted)
            .FirstAsync();

        long? bankAccountId = await _db.OwnerBankAccounts
            .Where(account => account.OwnerId == trip.OwnerId && account.IsPrimary && account.IsActive)
            .Select(account => (long?)account.OwnerBankAccountId)
            .FirstOrDefaultAsync();

        decimal freight = quote.VehicleCost + quote.DriverCost + quote.LoadingCharges;

        _db.Settlements.Add(new Settlement
        {
            TripId = trip.TripId,
            OwnerId = trip.OwnerId,
            OwnerBankAccountId = bankAccountId,
            GrossAmount = freight,
            CommissionAmount = quote.PlatformFee,
            TdsAmount = 0,
            NetAmount = freight - quote.PlatformFee,
            Status = SettlementStatus.AwaitingPod,
        });
    }

    private async Task CheckVehicleCanTakeBookingAsync(Vehicle vehicle, Booking booking, long? requiredOwnerId)
    {
        if (requiredOwnerId is long ownerId && vehicle.OwnerId != ownerId)
        {
            throw ApiException.Forbidden("That vehicle is not yours.");
        }

        Guard.Require(vehicle.VerificationStatus == VehicleVerification.Approved, "This vehicle is not verified yet.");
        Guard.Require(vehicle.AvailabilityStatus == VehicleAvailability.Available, "This vehicle is not available.");
        Guard.Require(vehicle.VehicleTypeId == booking.VehicleTypeId, "The booking needs a different vehicle type.");
        Guard.Require(vehicle.CapacityKg >= booking.WeightKg, "The load is heavier than this vehicle's capacity.");

        string ownerKyc = await _db.Owners
            .Where(owner => owner.OwnerId == vehicle.OwnerId)
            .Select(owner => owner.KycStatus)
            .FirstAsync();
        Guard.Require(ownerKyc == KycStatus.Approved, "The vehicle owner's KYC is not approved yet.");
    }

    private async Task CheckDriverCanDriveAsync(Driver driver, Vehicle vehicle)
    {
        Guard.Require(driver.OwnerId == vehicle.OwnerId, "The driver must be linked to the vehicle's owner.");
        Guard.Require(driver.KycStatus == KycStatus.Approved, "This driver is not verified yet.");
        Guard.Require(driver.LicenceExpiry > DateOnly.FromDateTime(DateTime.UtcNow), "This driver's licence has expired.");

        bool alreadyOnTrip = await _db.Trips.AnyAsync(trip =>
            trip.DriverId == driver.DriverId && !TripStatus.Finished.Contains(trip.Status));
        Guard.Require(!alreadyOnTrip, "This driver is already on a trip.");
    }
}
