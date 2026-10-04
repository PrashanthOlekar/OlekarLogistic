using FluentValidation;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Abstractions.Storage;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Auth;
using ProCargo.Application.Features.Bookings;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;
using ProCargo.Domain.Invoicing;
using ProCargo.Domain.Trips;

namespace ProCargo.Application.Features.Trips;

internal sealed class TripService(
    ITripRepository trips,
    IBookingRepository bookings,
    IVehicleRepository vehicles,
    IDriverRepository drivers,
    IOwnerRepository owners,
    IFileStorage storage,
    IPersonalDataProtector protector,
    ICurrentProfiles profiles,
    ICurrentUser currentUser,
    IAuditTrail auditTrail,
    TimeProvider clock,
    IValidator<AssignTripRequest> assignValidator,
    IValidator<TripEventRequest> eventValidator,
    IValidator<HandoverRequest> handoverValidator,
    IValidator<TripPhotoRequest> photoValidator,
    ILogger<TripService> logger) : ITripService
{
    private const string TripNotFound = "Trip not found.";

    // ---------------------------------------------------------------- read

    public async Task<PagedResult<TripListItem>> GetPagedAsync(TripQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Admin))
        {
            return await trips.GetPagedAsync(query, null, null, cancellationToken);
        }

        if (currentUser.IsInRole(Roles.Owner))
        {
            Owner owner = await profiles.GetOwnerAsync(cancellationToken);
            return await trips.GetPagedAsync(query, owner.OwnerId, null, cancellationToken);
        }

        // Drivers see their current trips first, and never the owner's payout.
        Driver driver = await profiles.GetDriverAsync(cancellationToken);
        query.Sort ??= "ActiveFirst";
        PagedResult<TripListItem> page = await trips.GetPagedAsync(query, null, driver.DriverId, cancellationToken);
        foreach (TripListItem item in page.Items)
        {
            item.OwnerPayout = null;
        }

        return page;
    }

    public async Task<TripDetail> GetAsync(long tripId, CancellationToken cancellationToken)
    {
        TripDetailData data = await trips.GetDetailAsync(tripId, cancellationToken)
            ?? throw new NotFoundException(TripNotFound);
        TripDetailRow row = data.Trip;

        bool canSee = currentUser.Role switch
        {
            Roles.Admin => true,
            Roles.Owner => row.OwnerId == (await profiles.GetOwnerAsync(cancellationToken)).OwnerId,
            Roles.Driver => row.DriverId == (await profiles.GetDriverAsync(cancellationToken)).DriverId,
            _ => false,
        };
        if (!canSee)
        {
            throw new NotFoundException(TripNotFound);
        }

        var booking = new TripBookingView(
            row.BookingNumber,
            row.GoodsDescription,
            row.WeightKg,
            row.PickupDate,
            row.PickupSlot,
            row.SpecialInstructions,
            new Stop(row.PickupCity, row.PickupAddress, row.PickupContactName, row.PickupContactPhone),
            new Stop(row.DropCity, row.DropAddress, row.DropContactName, row.DropContactPhone));

        return new TripDetail(row.Id, row.TripNumber, row.Status, row.Vehicle, row.HasPod, row.PlannedDistanceKm, row.DriverPay, booking, data.Events);
    }

    // ---------------------------------------------------------------- assignment

    public async Task<CreatedTrip> AssignAsync(AssignTripRequest request, CancellationToken cancellationToken)
    {
        await assignValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Owners may only use their own trucks, and only once their KYC is approved.
        long? requiredOwnerId = null;
        if (!currentUser.IsInRole(Roles.Admin))
        {
            Owner me = await profiles.GetOwnerAsync(cancellationToken);
            if (me.KycStatus != KycStatus.Approved)
            {
                throw new BusinessRuleException("Your KYC must be approved before you can take loads.");
            }
            requiredOwnerId = me.OwnerId;
        }

        Booking booking = await bookings.GetByIdAsync(request.BookingId, cancellationToken)
            ?? throw new NotFoundException("Booking not found.");
        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new ConflictException("This load has already been taken or is not paid yet.");
        }

        Vehicle vehicle = await vehicles.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException("Vehicle not found.");
        await CheckVehicleCanTakeBookingAsync(vehicle, booking, requiredOwnerId, cancellationToken);

        Driver driver = await drivers.GetByIdAsync(request.DriverId, cancellationToken)
            ?? throw new NotFoundException("Driver not found.");
        await CheckDriverCanDriveAsync(driver, vehicle, cancellationToken);

        var assignment = new TripAssignment(
            booking.BookingId,
            vehicle.VehicleId,
            driver.DriverId,
            requiredOwnerId,
            protector.Protect(OtpService.NewCode(4)),
            protector.Protect(OtpService.NewCode(4)),
            currentUser.UserId);

        CreatedTrip trip = await trips.AssignAsync(assignment, cancellationToken);

        string action = requiredOwnerId is null ? "Booking.AssignedByAdmin" : "Load.Accepted";
        await auditTrail.RecordAsync(action, "Booking", booking.BookingId, new { request.VehicleId, request.DriverId }, cancellationToken);
        logger.LogInformation(
            "Trip {TripNumber} created: booking {BookingId}, vehicle {VehicleId}, driver {DriverId}, by user {UserId}",
            trip.TripNumber, booking.BookingId, vehicle.VehicleId, driver.DriverId, currentUser.UserId);

        return trip;
    }

    private async Task CheckVehicleCanTakeBookingAsync(Vehicle vehicle, Booking booking, long? requiredOwnerId, CancellationToken cancellationToken)
    {
        if (requiredOwnerId is long ownerId && vehicle.OwnerId != ownerId)
        {
            throw new ForbiddenException("That vehicle is not yours.");
        }

        Require(vehicle.VerificationStatus == VehicleVerification.Approved, "This vehicle is not verified yet.");
        Require(vehicle.AvailabilityStatus == VehicleAvailability.Available, "This vehicle is not available.");
        Require(vehicle.VehicleTypeId == booking.VehicleTypeId, "The booking needs a different vehicle type.");
        Require(vehicle.CapacityKg >= booking.WeightKg, "The load is heavier than this vehicle's capacity.");

        Owner? vehicleOwner = await owners.GetByIdAsync(vehicle.OwnerId, cancellationToken);
        Require(vehicleOwner?.KycStatus == KycStatus.Approved, "The vehicle owner's KYC is not approved yet.");
    }

    private async Task CheckDriverCanDriveAsync(Driver driver, Vehicle vehicle, CancellationToken cancellationToken)
    {
        DateOnly today = IndianTime.TodayFor(clock.GetUtcNow().UtcDateTime);

        Require(driver.OwnerId == vehicle.OwnerId, "The driver must be linked to the vehicle's owner.");
        Require(driver.KycStatus == KycStatus.Approved, "This driver is not verified yet.");
        Require(driver.LicenceExpiry > today, "This driver's licence has expired.");

        bool alreadyOnTrip = await drivers.HasActiveTripAsync(driver.DriverId, cancellationToken);
        Require(!alreadyOnTrip, "This driver is already on a trip.");
    }

    // ---------------------------------------------------------------- driver steps

    public async Task RecordEventAsync(long tripId, TripEventRequest request, CancellationToken cancellationToken)
    {
        await eventValidator.ValidateAndThrowAsync(request, cancellationToken);
        Trip trip = await GetOwnTripAsync(tripId, cancellationToken);

        TripStep step = TripSteps.Find(request.Action)!;
        if (!step.AllowedFrom.Contains(trip.Status))
        {
            throw new ConflictException($"This step isn't available while the trip is {trip.Status}.");
        }

        await trips.ChangeStatusAsync(
            new TripStatusChange(trip.TripId, step.AllowedFrom, step.NextStatus, step.EventType, null, request.Latitude, request.Longitude, currentUser.UserId),
            cancellationToken);

        logger.LogInformation("Trip {TripId} moved from {From} to {To}", trip.TripId, trip.Status, step.NextStatus);
    }

    /// <summary>
    /// The sender reads out the pickup code; the receiver reads out the delivery code.
    /// The customer sees both codes in their booking.
    /// </summary>
    public async Task ConfirmHandoverAsync(long tripId, HandoverRequest request, CancellationToken cancellationToken)
    {
        await handoverValidator.ValidateAndThrowAsync(request, cancellationToken);
        Trip trip = await GetOwnTripAsync(tripId, cancellationToken);
        string code = request.Code.Trim();

        if (request.Kind == HandoverKind.Pickup)
        {
            if (trip.Status != TripStatus.AtPickup)
            {
                throw new ConflictException("Mark \"Reached pickup\" first.");
            }
            Require(CodeMatches(trip.PickupOtpProtected, code), "That code doesn't match. Ask the sender to check it.");

            TripStep step = TripSteps.PickupHandover;
            await trips.ChangeStatusAsync(
                new TripStatusChange(trip.TripId, step.AllowedFrom, step.NextStatus, step.EventType, "Goods handed over", null, null, currentUser.UserId),
                cancellationToken);

            logger.LogInformation("Trip {TripId} loaded (pickup code confirmed)", trip.TripId);
            return;
        }

        if (trip.Status != TripStatus.AtDestination)
        {
            throw new ConflictException("Mark \"Reached destination\" first.");
        }
        Require(await trips.HasPodAsync(trip.TripId, cancellationToken), "Upload the signed delivery receipt (POD) first.");
        Require(CodeMatches(trip.DeliveryOtpProtected, code), "That code doesn't match. Ask the receiver to check it.");

        await trips.CompleteDeliveryAsync(trip.TripId, currentUser.UserId, cancellationToken);
        logger.LogInformation("Trip {TripId} delivered (delivery code confirmed)", trip.TripId);
    }

    public async Task<CreatedResource> AddPhotoAsync(long tripId, TripPhotoRequest request, FileUpload? file, CancellationToken cancellationToken)
    {
        await photoValidator.ValidateAndThrowAsync(request, cancellationToken);
        Trip trip = await GetOwnTripAsync(tripId, cancellationToken);

        if (request.Kind == DocumentType.Pod)
        {
            Require(trip.Status == TripStatus.AtDestination, "Upload the POD after you reach the destination.");
        }
        else
        {
            Require(trip.Status is TripStatus.AtPickup or TripStatus.Loaded, "Upload the goods photo at pickup.");
        }

        FileUploadRules.Check(file);
        StoredFile saved = await storage.SaveAsync(file!, cancellationToken);

        long documentId = await trips.AddPhotoAsync(
            new TripPhoto(
                trip.TripId,
                request.Kind,
                saved.Path,
                saved.FileName,
                saved.ContentType,
                saved.SizeBytes,
                saved.Sha256,
                request.Latitude,
                request.Longitude,
                currentUser.UserId),
            cancellationToken);

        logger.LogInformation("Trip {TripId}: {Kind} uploaded as document {DocumentId}", trip.TripId, request.Kind, documentId);
        return new CreatedResource(documentId);
    }

    // ---------------------------------------------------------------- POD approval

    /// <summary>
    /// The admin has checked the signed delivery receipt. This, in one transaction:
    ///   1. completes the trip and the booking,
    ///   2. approves the owner's payout (it can now be released),
    ///   3. marks the POD documents as verified,
    ///   4. issues the customer's GST invoice.
    /// </summary>
    public async Task ApprovePodAsync(long tripId, CancellationToken cancellationToken)
    {
        PodApprovalData data = await trips.GetForPodApprovalAsync(tripId, cancellationToken)
            ?? throw new NotFoundException(TripNotFound);

        Require(data.Status == TripStatus.Delivered, "Only delivered trips can have their POD approved.");
        if (data.TaxAmount is not decimal taxAmount || data.TotalAmount is not decimal totalAmount)
        {
            throw new ConflictException("This trip's booking has no accepted quote.");
        }

        long sequence = await trips.NextInvoiceSequenceAsync(cancellationToken);
        DateOnly indianToday = IndianTime.TodayFor(clock.GetUtcNow().UtcDateTime);
        GstSplit gst = GstSplit.For(taxAmount, data.PickupState, data.DropState);

        var approval = new PodApproval(
            data.TripId,
            currentUser.UserId,
            InvoiceNumber.Format(sequence, indianToday),
            data.PickupState ?? "Karnataka",
            data.CustomerGstin,
            TaxableAmount: totalAmount - taxAmount,
            gst.Cgst,
            gst.Sgst,
            gst.Igst,
            totalAmount);

        await trips.ApprovePodAsync(approval, cancellationToken);

        await auditTrail.RecordAsync("Trip.PodApproved", "Trip", tripId, new { approval.InvoiceNumber }, cancellationToken);
        logger.LogInformation("Trip {TripId} POD approved; invoice {InvoiceNumber} issued", tripId, approval.InvoiceNumber);
    }

    // ---------------------------------------------------------------- helpers

    private async Task<Trip> GetOwnTripAsync(long tripId, CancellationToken cancellationToken)
    {
        Driver driver = await profiles.GetDriverAsync(cancellationToken);
        Trip? trip = await trips.GetByIdAsync(tripId, cancellationToken);

        return trip is not null && trip.DriverId == driver.DriverId
            ? trip
            : throw new NotFoundException(TripNotFound);
    }

    private bool CodeMatches(string? protectedCode, string enteredCode) =>
        protectedCode is not null && protector.Unprotect(protectedCode) == enteredCode;

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new BusinessRuleException(message);
        }
    }
}
