using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Common;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Pricing;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;
using ProCargo.Domain.Trips;

namespace ProCargo.Application.Features.Bookings;

internal sealed class BookingService(
    IBookingRepository bookings,
    IReferenceDataRepository referenceData,
    IQuotePricer pricer,
    ICurrentProfiles profiles,
    ICurrentUser currentUser,
    IPersonalDataProtector protector,
    IAuditTrail auditTrail,
    IOptions<PaymentOptions> paymentOptions,
    TimeProvider clock,
    IValidator<CreateBookingRequest> createValidator,
    IValidator<PayBookingRequest> payValidator,
    IValidator<CancelBookingRequest> cancelValidator,
    ILogger<BookingService> logger) : IBookingService
{
    private const string NotFoundMessage = "Booking not found.";

    public async Task<PagedResult<BookingListItem>> GetPagedAsync(BookingQuery query, CancellationToken cancellationToken)
    {
        long? customerId = currentUser.IsInRole(Roles.Admin)
            ? null
            : await profiles.GetCustomerIdAsync(cancellationToken);

        return await bookings.GetPagedAsync(query, customerId, cancellationToken);
    }

    public async Task<BookingDetail> GetAsync(long bookingId, CancellationToken cancellationToken)
    {
        BookingDetailData data = await bookings.GetDetailAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        bool isAdmin = currentUser.IsInRole(Roles.Admin);
        if (!isAdmin && data.Booking.CustomerId != await profiles.GetCustomerIdAsync(cancellationToken))
        {
            throw new NotFoundException(NotFoundMessage);
        }

        // Handover codes are for the customer who booked, never for anyone else.
        return BuildDetail(data, showCodes: !isAdmin);
    }

    public async Task<CreatedBooking> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        long customerId = await profiles.GetCustomerIdAsync(cancellationToken);

        City from = await referenceData.GetCityAsync(request.PickupCityId, cancellationToken)
            ?? throw new BusinessRuleException("Choose a pickup city.");
        City to = await referenceData.GetCityAsync(request.DropCityId, cancellationToken)
            ?? throw new BusinessRuleException("Choose a delivery city.");
        VehicleType vehicleType = await referenceData.GetVehicleTypeAsync(request.VehicleTypeId, cancellationToken)
            ?? throw new BusinessRuleException("Choose a vehicle type.");
        _ = await referenceData.GetGoodsCategoryAsync(request.GoodsCategoryId, cancellationToken)
            ?? throw new BusinessRuleException("Choose what you are sending.");

        if (request.WeightKg > vehicleType.MaxLoadKg)
        {
            throw new BusinessRuleException(
                $"{vehicleType.Name} carries up to {vehicleType.MaxLoadKg:N0} kg. Choose a larger vehicle.");
        }

        var booking = new NewBooking(
            customerId,
            from.CityId,
            request.PickupAddress.Trim(),
            from.Latitude,
            from.Longitude,
            Text.Clean(request.PickupContactName),
            Text.Clean(request.PickupContactPhone),
            to.CityId,
            request.DropAddress.Trim(),
            to.Latitude,
            to.Longitude,
            Text.Clean(request.DropContactName),
            Text.Clean(request.DropContactPhone),
            request.GoodsCategoryId,
            request.GoodsDescription.Trim(),
            request.WeightKg,
            request.GoodsValue,
            vehicleType.VehicleTypeId,
            request.PickupDate,
            Text.Clean(request.PickupSlot),
            Text.Clean(request.SpecialInstructions));

        Quote quote = await pricer.CreateQuoteAsync(vehicleType, from, to, cancellationToken);
        CreatedBooking created = await bookings.CreateAsync(booking, quote, cancellationToken);

        logger.LogInformation(
            "Booking {BookingNumber} ({BookingId}) created by customer {CustomerId}",
            created.BookingNumber, created.Id, customerId);

        return created;
    }

    public async Task<CreatedResource> RequoteAsync(long bookingId, CancellationToken cancellationToken)
    {
        Booking booking = await GetOwnBookingAsync(bookingId, cancellationToken);
        if (booking.Status != BookingStatus.Quoted)
        {
            throw new BusinessRuleException("Only unpaid bookings can be re-quoted.");
        }

        City from = await referenceData.GetCityAsync(booking.PickupCityId ?? 0, cancellationToken)
            ?? throw new BusinessRuleException("The pickup city is no longer available.");
        City to = await referenceData.GetCityAsync(booking.DropCityId ?? 0, cancellationToken)
            ?? throw new BusinessRuleException("The delivery city is no longer available.");
        VehicleType vehicleType = await referenceData.GetVehicleTypeAsync(booking.VehicleTypeId, cancellationToken)
            ?? throw new BusinessRuleException("This vehicle type is no longer available.");

        Quote quote = await pricer.CreateQuoteAsync(vehicleType, from, to, cancellationToken);
        long quoteId = await bookings.RequoteAsync(bookingId, quote, cancellationToken);

        logger.LogInformation("Booking {BookingId} re-quoted at {Total}", bookingId, quote.TotalAmount);
        return new CreatedResource(quoteId);
    }

    /// <summary>
    /// TEST MODE ONLY: marks the quote as paid straight away.
    /// To go live, replace with your payment gateway:
    /// create an order → the customer pays in the gateway's checkout → a signed webhook marks it Captured.
    /// </summary>
    public async Task<PaymentReceipt> PayAsync(long bookingId, PayBookingRequest request, CancellationToken cancellationToken)
    {
        if (!paymentOptions.Value.IsTestMode)
        {
            throw new FeatureUnavailableException("Online payment is not connected yet. Add your payment gateway keys to enable it.");
        }

        await payValidator.ValidateAndThrowAsync(request, cancellationToken);

        Booking booking = await GetOwnBookingAsync(bookingId, cancellationToken);
        if (booking.Status != BookingStatus.Quoted)
        {
            throw new ConflictException("This booking is already paid or closed.");
        }

        Quote quote = await bookings.GetLatestOpenQuoteAsync(bookingId, cancellationToken)
            ?? throw new BusinessRuleException("Get a fresh quote first.");
        if (quote.ValidUntil < clock.GetUtcNow().UtcDateTime)
        {
            throw new BusinessRuleException("This quote has expired. Get a fresh quote first.");
        }

        var payment = new PaymentRecord(
            bookingId,
            quote.QuoteId,
            request.Method,
            Gateway: "Test",
            GatewayOrderId: "test_order_" + Guid.NewGuid().ToString("N")[..16],
            GatewayPaymentId: "test_pay_" + Guid.NewGuid().ToString("N")[..16]);

        PaymentReceipt receipt = await bookings.PayAsync(payment, cancellationToken);

        await auditTrail.RecordAsync("Booking.Paid", "Booking", bookingId, new { quote.TotalAmount, request.Method }, cancellationToken);
        logger.LogInformation("Booking {BookingId} paid ₹{Amount} by {Method}", bookingId, receipt.Amount, request.Method);

        return receipt;
    }

    public async Task CancelAsync(long bookingId, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        await cancelValidator.ValidateAndThrowAsync(request, cancellationToken);

        Booking booking = await GetOwnBookingAsync(bookingId, cancellationToken);
        if (!BookingStatus.CanBeCancelled(booking.Status))
        {
            throw new BusinessRuleException("Goods are already loaded, so this booking can't be cancelled online. Call support.");
        }

        string? reason = Text.Clean(request.Reason);
        await bookings.CancelAsync(bookingId, currentUser.UserId, reason, cancellationToken);

        await auditTrail.RecordAsync("Booking.Cancelled", "Booking", bookingId, new { Reason = reason }, cancellationToken);
        logger.LogInformation("Booking {BookingId} cancelled by user {UserId}", bookingId, currentUser.UserId);
    }

    public async Task<IReadOnlyList<AssignableVehicle>> GetAssignableVehiclesAsync(long bookingId, CancellationToken cancellationToken)
    {
        _ = await bookings.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        return await bookings.GetAssignableVehiclesAsync(bookingId, cancellationToken);
    }

    private async Task<Booking> GetOwnBookingAsync(long bookingId, CancellationToken cancellationToken)
    {
        long customerId = await profiles.GetCustomerIdAsync(cancellationToken);
        Booking? booking = await bookings.GetByIdAsync(bookingId, cancellationToken);

        return booking is not null && booking.CustomerId == customerId
            ? booking
            : throw new NotFoundException(NotFoundMessage);
    }

    private BookingDetail BuildDetail(BookingDetailData data, bool showCodes)
    {
        BookingDetailRow row = data.Booking;
        DateTime now = clock.GetUtcNow().UtcDateTime;

        BookingQuoteView? quote = data.LatestQuote is not { } latest
            ? null
            : new BookingQuoteView(
                latest.DistanceKm,
                latest.VehicleCost,
                latest.DriverCost,
                latest.LoadingCharges,
                latest.TaxAmount,
                latest.TotalAmount,
                latest.ValidUntil,
                latest.Status,
                Expired: latest.Status == QuoteStatus.Sent && latest.ValidUntil < now);

        BookingTripView? trip = data.Trip is not { } activeTrip
            ? null
            : new BookingTripView(
                activeTrip.TripNumber,
                activeTrip.Status,
                activeTrip.Vehicle,
                activeTrip.DriverName,
                activeTrip.DriverMobile,
                PickupOtp: showCodes && HandoverCodeVisibility.ShowPickupCode(activeTrip.Status) && activeTrip.PickupOtpProtected is not null
                    ? protector.Unprotect(activeTrip.PickupOtpProtected)
                    : null,
                DeliveryOtp: showCodes && HandoverCodeVisibility.ShowDeliveryCode(activeTrip.Status) && activeTrip.DeliveryOtpProtected is not null
                    ? protector.Unprotect(activeTrip.DeliveryOtpProtected)
                    : null,
                data.Events);

        return new BookingDetail(
            row.Id,
            row.BookingNumber,
            row.Status,
            row.PickupDate,
            row.PickupSlot,
            row.WeightKg,
            row.GoodsValue,
            row.GoodsDescription,
            row.SpecialInstructions,
            row.CreatedAt,
            row.CancelledReason,
            row.Goods,
            row.VehicleType,
            new Stop(row.PickupCity, row.PickupAddress, row.PickupContactName, row.PickupContactPhone),
            new Stop(row.DropCity, row.DropAddress, row.DropContactName, row.DropContactPhone),
            quote,
            data.Payment,
            trip,
            data.Invoice);
    }
}
