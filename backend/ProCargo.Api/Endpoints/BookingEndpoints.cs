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
/// Customer bookings. Customers only ever see their own bookings.
///   POST /api/bookings               create a booking (priced straight away)
///   GET  /api/bookings               my bookings
///   GET  /api/bookings/{id}          one booking with price, truck, codes and invoice
///   POST /api/bookings/{id}/requote  new price after the quote expired
///   POST /api/bookings/{id}/pay      pay the quote (test mode until a gateway is connected)
///   POST /api/bookings/{id}/cancel   cancel before loading
/// </summary>
public static class BookingEndpoints
{
    private static readonly string[] PaymentMethods = { "UPI", "CreditCard", "DebitCard", "NetBanking", "Wallet" };

    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/bookings").RequireAuthorization(Roles.Customer);

        group.MapPost("/", CreateBookingAsync);
        group.MapGet("/", ListMyBookingsAsync);
        group.MapGet("/{id:long}", GetBookingAsync);
        group.MapPost("/{id:long}/requote", RequoteAsync);
        group.MapPost("/{id:long}/pay", PayAsync);
        group.MapPost("/{id:long}/cancel", CancelAsync);
    }

    // ---------------------------------------------------------------- create and list

    private static async Task<IResult> CreateBookingAsync(
        CreateBookingRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        PricingService pricing)
    {
        long customerId = await GetCustomerIdAsync(db, principal);

        City from = await db.Cities.FindAsync(request.PickupCityId) ?? throw ApiException.BadRequest("Choose a pickup city.");
        City to = await db.Cities.FindAsync(request.DropCityId) ?? throw ApiException.BadRequest("Choose a delivery city.");
        VehicleType vehicleType = await db.VehicleTypes.FindAsync(request.VehicleTypeId)
            ?? throw ApiException.BadRequest("Choose a vehicle type.");

        bool goodsCategoryExists = await db.GoodsCategories.AnyAsync(category => category.GoodsCategoryId == request.GoodsCategoryId);
        Guard.Require(goodsCategoryExists, "Choose what you are sending.");
        Guard.Require(
            !string.IsNullOrWhiteSpace(request.PickupAddress) && !string.IsNullOrWhiteSpace(request.DropAddress),
            "Enter both the pickup and delivery addresses.");
        Guard.Require(!string.IsNullOrWhiteSpace(request.GoodsDescription), "Describe the goods, for example \"40 cartons of biscuits\".");
        Guard.Require(request.WeightKg > 0, "Enter the approximate weight in kg.");
        Guard.Require(
            request.WeightKg <= vehicleType.MaxLoadKg,
            $"{vehicleType.Name} carries up to {vehicleType.MaxLoadKg:N0} kg. Choose a larger vehicle.");
        Guard.Require(request.PickupDate >= IndianTime.Today, "The pickup date can't be in the past.");

        var booking = new Booking
        {
            CustomerId = customerId,
            PickupCityId = from.CityId,
            PickupAddressText = request.PickupAddress.Trim(),
            PickupLatitude = from.Latitude,
            PickupLongitude = from.Longitude,
            PickupContactName = request.PickupContactName,
            PickupContactPhone = request.PickupContactPhone,
            DropCityId = to.CityId,
            DropAddressText = request.DropAddress.Trim(),
            DropLatitude = to.Latitude,
            DropLongitude = to.Longitude,
            DropContactName = request.DropContactName,
            DropContactPhone = request.DropContactPhone,
            GoodsCategoryId = request.GoodsCategoryId,
            GoodsDescription = request.GoodsDescription.Trim(),
            WeightKg = request.WeightKg,
            GoodsValue = request.GoodsValue,
            VehicleTypeId = vehicleType.VehicleTypeId,
            PickupDate = request.PickupDate,
            PickupSlot = request.PickupSlot,
            SpecialInstructions = request.SpecialInstructions,
            Status = BookingStatus.Quoted,
        };

        decimal distanceKm = PricingService.EstimateRoadKm(from, to);
        booking.Quotes.Add(await pricing.CreateQuoteAsync(vehicleType, distanceKm));

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        return Results.Created($"/api/bookings/{booking.BookingId}", new { id = booking.BookingId, booking.BookingNumber });
    }

    private static async Task<IResult> ListMyBookingsAsync(ClaimsPrincipal principal, ProCargoDbContext db)
    {
        long customerId = await GetCustomerIdAsync(db, principal);

        var bookings = await db.Bookings
            .Where(booking => booking.CustomerId == customerId)
            .OrderByDescending(booking => booking.CreatedAt)
            .Select(booking => new
            {
                id = booking.BookingId,
                booking.BookingNumber,
                booking.Status,
                booking.PickupDate,
                booking.WeightKg,
                booking.CreatedAt,
                from = booking.PickupCity!.Name,
                to = booking.DropCity!.Name,
                vehicleType = booking.VehicleType.Name,
                total = booking.Quotes
                    .OrderByDescending(quote => quote.CreatedAt)
                    .Select(quote => (decimal?)quote.TotalAmount)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        return Results.Ok(bookings);
    }

    // ---------------------------------------------------------------- booking detail

    private static async Task<IResult> GetBookingAsync(
        long id,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        PersonalDataProtector protector)
    {
        long customerId = await GetCustomerIdAsync(db, principal);

        Booking booking = await db.Bookings
            .Include(row => row.PickupCity)
            .Include(row => row.DropCity)
            .Include(row => row.VehicleType)
            .Include(row => row.GoodsCategory)
            .FirstOrDefaultAsync(row => row.BookingId == id && row.CustomerId == customerId)
            ?? throw ApiException.NotFound("Booking not found.");

        Quote? quote = await db.Quotes
            .Where(row => row.BookingId == id)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync();

        var payment = await db.Payments
            .Where(row => row.BookingId == id)
            .OrderByDescending(row => row.CreatedAt)
            .Select(row => new { row.Method, row.Gateway, row.Status, row.Amount, row.PaidAt, row.GatewayPaymentId })
            .FirstOrDefaultAsync();

        var invoice = await db.Invoices
            .Where(row => row.BookingId == id)
            .Select(row => new { row.InvoiceNumber, row.TaxableAmount, row.CGST, row.SGST, row.IGST, row.TotalAmount, row.IssuedAt })
            .FirstOrDefaultAsync();

        Trip? trip = await db.Trips
            .Include(row => row.Vehicle)
            .Include(row => row.Driver).ThenInclude(driver => driver.User)
            .Include(row => row.Events)
            .FirstOrDefaultAsync(row => row.BookingId == id && row.Status != TripStatus.Cancelled);

        return Results.Ok(new
        {
            id = booking.BookingId,
            booking.BookingNumber,
            booking.Status,
            booking.PickupDate,
            booking.PickupSlot,
            booking.WeightKg,
            booking.GoodsValue,
            booking.GoodsDescription,
            booking.SpecialInstructions,
            booking.CreatedAt,
            booking.CancelledReason,
            goods = booking.GoodsCategory.Name,
            vehicleType = booking.VehicleType.Name,
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
            quote = quote is null ? null : new
            {
                quote.DistanceKm,
                quote.VehicleCost,
                quote.DriverCost,
                quote.LoadingCharges,
                quote.TaxAmount,
                quote.TotalAmount,
                quote.ValidUntil,
                quote.Status,
                expired = quote.Status == QuoteStatus.Sent && quote.ValidUntil < DateTime.UtcNow,
            },
            payment,
            trip = trip is null ? null : BuildTripView(trip, protector),
            invoice,
        });
    }

    /// <summary>
    /// Truck, driver and timeline for the customer.
    /// The pickup code is shown until the goods are loaded; the delivery code until delivery.
    /// </summary>
    private static object BuildTripView(Trip trip, PersonalDataProtector protector)
    {
        bool showPickupCode = trip.Status is TripStatus.Assigned or TripStatus.EnRouteToPickup or TripStatus.AtPickup;
        bool showDeliveryCode = trip.Status is not (TripStatus.Delivered or TripStatus.Completed);

        return new
        {
            trip.TripNumber,
            trip.Status,
            vehicle = trip.Vehicle.RegistrationNumber,
            driverName = trip.Driver.User.FullName,
            driverMobile = trip.Driver.User.Mobile,
            pickupOtp = showPickupCode && trip.PickupOtpProtected != null
                ? protector.Unprotect(trip.PickupOtpProtected)
                : null,
            deliveryOtp = showDeliveryCode && trip.DeliveryOtpProtected != null
                ? protector.Unprotect(trip.DeliveryOtpProtected)
                : null,
            events = trip.Events
                .OrderBy(tripEvent => tripEvent.CreatedAt)
                .Select(tripEvent => new { tripEvent.EventType, tripEvent.Note, tripEvent.CreatedAt }),
        };
    }

    // ---------------------------------------------------------------- price, payment, cancellation

    private static async Task<IResult> RequoteAsync(
        long id,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        PricingService pricing)
    {
        Booking booking = await GetOwnBookingAsync(db, principal, id);
        Guard.Require(booking.Status == BookingStatus.Quoted, "Only unpaid bookings can be re-quoted.");

        List<Quote> openQuotes = await db.Quotes
            .Where(quote => quote.BookingId == id && quote.Status == QuoteStatus.Sent)
            .ToListAsync();
        foreach (Quote oldQuote in openQuotes)
        {
            oldQuote.Status = oldQuote.ValidUntil < DateTime.UtcNow ? QuoteStatus.Expired : QuoteStatus.Superseded;
        }

        City from = await db.Cities.FirstAsync(city => city.CityId == booking.PickupCityId);
        City to = await db.Cities.FirstAsync(city => city.CityId == booking.DropCityId);
        VehicleType vehicleType = await db.VehicleTypes.FirstAsync(type => type.VehicleTypeId == booking.VehicleTypeId);

        Quote newQuote = await pricing.CreateQuoteAsync(vehicleType, PricingService.EstimateRoadKm(from, to));
        newQuote.BookingId = id;
        db.Quotes.Add(newQuote);

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    /// <summary>
    /// TEST MODE ONLY: marks the quote as paid straight away.
    /// To go live, replace with your payment gateway:
    /// create an order → the customer pays in the gateway's checkout → a signed webhook marks it Captured.
    /// </summary>
    private static async Task<IResult> PayAsync(
        long id,
        PayRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        IConfiguration configuration,
        HttpContext http)
    {
        bool testMode = string.Equals(configuration["Payments:Mode"], "Test", StringComparison.OrdinalIgnoreCase);
        if (!testMode)
        {
            throw new ApiException(StatusCodes.Status501NotImplemented, "Online payment is not connected yet. Add your payment gateway keys to enable it.");
        }

        Guard.Require(PaymentMethods.Contains(request.Method), "Choose a payment method.");

        Booking booking = await GetOwnBookingAsync(db, principal, id);
        Guard.Require(booking.Status == BookingStatus.Quoted, "This booking is already paid or closed.");

        Quote quote = await db.Quotes
            .Where(row => row.BookingId == id && row.Status == QuoteStatus.Sent)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw ApiException.BadRequest("Get a fresh quote first.");
        Guard.Require(quote.ValidUntil >= DateTime.UtcNow, "This quote has expired. Get a fresh quote first.");

        db.Payments.Add(new Payment
        {
            BookingId = id,
            CustomerId = booking.CustomerId,
            QuoteId = quote.QuoteId,
            Amount = quote.TotalAmount,
            Method = request.Method,
            Gateway = "Test",
            GatewayOrderId = "test_order_" + Guid.NewGuid().ToString("N")[..16],
            GatewayPaymentId = "test_pay_" + Guid.NewGuid().ToString("N")[..16],
            Status = PaymentStatus.Captured,
            PaidAt = DateTime.UtcNow,
        });

        quote.Status = QuoteStatus.Accepted;
        quote.AcceptedAt = DateTime.UtcNow;

        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAt = DateTime.UtcNow;

        AuditLogger.Log(db, http, "Booking.Paid", "Booking", id, new { quote.TotalAmount, request.Method });
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> CancelAsync(
        long id,
        CancelBookingRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        HttpContext http)
    {
        const string tooLateMessage = "Goods are already loaded, so this booking can't be cancelled online. Call support.";

        Booking booking = await GetOwnBookingAsync(db, principal, id);
        bool bookingCanBeCancelled = booking.Status is BookingStatus.QuotePending
            or BookingStatus.Quoted
            or BookingStatus.Confirmed
            or BookingStatus.Assigned;
        Guard.Require(bookingCanBeCancelled, tooLateMessage);

        long userId = principal.GetUserId();

        Trip? trip = await db.Trips
            .Include(row => row.Vehicle)
            .Include(row => row.Driver)
            .FirstOrDefaultAsync(row => row.BookingId == id && row.Status != TripStatus.Cancelled);

        if (trip is not null)
        {
            bool notLoadedYet = trip.Status is TripStatus.Assigned or TripStatus.EnRouteToPickup or TripStatus.AtPickup;
            Guard.Require(notLoadedYet, tooLateMessage);

            trip.Status = TripStatus.Cancelled;
            trip.UpdatedAt = DateTime.UtcNow;
            trip.Vehicle.AvailabilityStatus = VehicleAvailability.Available;
            trip.Driver.DutyStatus = DutyStatus.Available;

            db.TripEvents.Add(new TripEvent
            {
                TripId = trip.TripId,
                EventType = TripEventType.Cancelled,
                Note = "Cancelled by customer",
                CreatedBy = userId,
            });
        }

        // Test mode: mark payments refunded. With a real gateway, call its refund API and add a Refunds row.
        List<Payment> payments = await db.Payments
            .Where(payment => payment.BookingId == id && payment.Status == PaymentStatus.Captured)
            .ToListAsync();
        foreach (Payment payment in payments)
        {
            payment.Status = PaymentStatus.Refunded;
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledBy = userId;
        booking.CancelledReason = request.Reason;
        booking.UpdatedAt = DateTime.UtcNow;

        AuditLogger.Log(db, http, "Booking.Cancelled", "Booking", id, new { request.Reason });
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // ---------------------------------------------------------------- helpers

    private static async Task<long> GetCustomerIdAsync(ProCargoDbContext db, ClaimsPrincipal principal)
    {
        long userId = principal.GetUserId();

        long? customerId = await db.Customers
            .Where(customer => customer.UserId == userId)
            .Select(customer => (long?)customer.CustomerId)
            .FirstOrDefaultAsync();

        return customerId ?? throw ApiException.Forbidden("Customer profile not found.");
    }

    private static async Task<Booking> GetOwnBookingAsync(ProCargoDbContext db, ClaimsPrincipal principal, long bookingId)
    {
        long customerId = await GetCustomerIdAsync(db, principal);

        return await db.Bookings.FirstOrDefaultAsync(booking => booking.BookingId == bookingId && booking.CustomerId == customerId)
            ?? throw ApiException.NotFound("Booking not found.");
    }
}
