using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Olekar.Api.Data;
using Olekar.Api.Services;

namespace Olekar.Api.Endpoints;

public record CreateBookingRequest(
    int PickupCityId, string PickupAddress, string? PickupContactName, string? PickupContactPhone,
    int DropCityId, string DropAddress, string? DropContactName, string? DropContactPhone,
    int GoodsCategoryId, string GoodsDescription, int WeightKg, decimal? GoodsValue,
    int VehicleTypeId, DateOnly PickupDate, string? PickupSlot, string? SpecialInstructions);
public record PayRequest(string Method);
public record CancelRequest(string? Reason);

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/bookings").RequireAuthorization(Roles.Customer);

        g.MapPost("/", async (CreateBookingRequest r, ClaimsPrincipal me, OlekarDbContext db, PricingService pricing, SettingsService settings) =>
        {
            var customerId = await CustomerId(db, me);
            var from = await db.Cities.FindAsync(r.PickupCityId) ?? throw ApiException.BadRequest("Choose a pickup city.");
            var to = await db.Cities.FindAsync(r.DropCityId) ?? throw ApiException.BadRequest("Choose a delivery city.");
            var vt = await db.VehicleTypes.FindAsync(r.VehicleTypeId) ?? throw ApiException.BadRequest("Choose a vehicle type.");
            Guard.Require(await db.GoodsCategories.AnyAsync(x => x.GoodsCategoryId == r.GoodsCategoryId), "Choose what you are sending.");
            Guard.Require(!string.IsNullOrWhiteSpace(r.PickupAddress) && !string.IsNullOrWhiteSpace(r.DropAddress), "Enter both the pickup and delivery addresses.");
            Guard.Require(!string.IsNullOrWhiteSpace(r.GoodsDescription), "Describe the goods, for example \"40 cartons of biscuits\".");
            Guard.Require(r.WeightKg > 0, "Enter the approximate weight in kg.");
            Guard.Require(r.WeightKg <= vt.MaxLoadKg, $"{vt.Name} carries up to {vt.MaxLoadKg:N0} kg. Choose a larger vehicle.");
            Guard.Require(r.PickupDate >= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5)), "The pickup date can't be in the past.");

            var booking = new Booking
            {
                CustomerId = customerId,
                PickupCityId = from.CityId, PickupAddressText = r.PickupAddress.Trim(), PickupLatitude = from.Latitude, PickupLongitude = from.Longitude,
                PickupContactName = r.PickupContactName, PickupContactPhone = r.PickupContactPhone,
                DropCityId = to.CityId, DropAddressText = r.DropAddress.Trim(), DropLatitude = to.Latitude, DropLongitude = to.Longitude,
                DropContactName = r.DropContactName, DropContactPhone = r.DropContactPhone,
                GoodsCategoryId = r.GoodsCategoryId, GoodsDescription = r.GoodsDescription.Trim(), WeightKg = r.WeightKg, GoodsValue = r.GoodsValue,
                VehicleTypeId = vt.VehicleTypeId, PickupDate = r.PickupDate, PickupSlot = r.PickupSlot, SpecialInstructions = r.SpecialInstructions,
                Status = "Quoted"
            };
            booking.Quotes.Add(await NewQuote(vt, PricingService.RoadKm(from, to), pricing, settings));
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            return Results.Created($"/api/bookings/{booking.BookingId}", new { id = booking.BookingId, booking.BookingNumber });
        });

        g.MapGet("/", async (ClaimsPrincipal me, OlekarDbContext db) =>
        {
            var customerId = await CustomerId(db, me);
            var rows = await db.Bookings.Where(b => b.CustomerId == customerId).OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    id = b.BookingId, b.BookingNumber, b.Status, b.PickupDate, b.WeightKg, b.CreatedAt,
                    from = b.PickupCity!.Name, to = b.DropCity!.Name, vehicleType = b.VehicleType.Name,
                    total = b.Quotes.OrderByDescending(q => q.CreatedAt).Select(q => (decimal?)q.TotalAmount).FirstOrDefault()
                }).ToListAsync();
            return rows;
        });

        g.MapGet("/{id:long}", async (long id, ClaimsPrincipal me, OlekarDbContext db, PiiProtector pii) =>
        {
            var customerId = await CustomerId(db, me);
            var b = await db.Bookings.Include(x => x.PickupCity).Include(x => x.DropCity).Include(x => x.VehicleType).Include(x => x.GoodsCategory)
                .FirstOrDefaultAsync(x => x.BookingId == id && x.CustomerId == customerId) ?? throw ApiException.NotFound("Booking not found.");
            var quote = await db.Quotes.Where(q => q.BookingId == id).OrderByDescending(q => q.CreatedAt).FirstOrDefaultAsync();
            var payment = await db.Payments.Where(p => p.BookingId == id).OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.Method, p.Gateway, p.Status, p.Amount, p.PaidAt, p.GatewayPaymentId }).FirstOrDefaultAsync();
            var trip = await db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).ThenInclude(d => d.User).Include(t => t.Events)
                .Where(t => t.BookingId == id && t.Status != "Cancelled").FirstOrDefaultAsync();
            var invoice = await db.Invoices.Where(i => i.BookingId == id)
                .Select(i => new { i.InvoiceNumber, i.TaxableAmount, i.CGST, i.SGST, i.IGST, i.TotalAmount, i.IssuedAt }).FirstOrDefaultAsync();

            object? tripView = null;
            if (trip is not null)
            {
                var showPickup = trip.Status is "Assigned" or "EnRouteToPickup" or "AtPickup";
                var showDelivery = trip.Status is not ("Delivered" or "Completed");
                tripView = new
                {
                    trip.TripNumber, trip.Status, vehicle = trip.Vehicle.RegistrationNumber,
                    driverName = trip.Driver.User.FullName, driverMobile = trip.Driver.User.Mobile,
                    pickupOtp = showPickup && trip.PickupOtpProtected != null ? pii.Unprotect(trip.PickupOtpProtected) : null,
                    deliveryOtp = showDelivery && trip.DeliveryOtpProtected != null ? pii.Unprotect(trip.DeliveryOtpProtected) : null,
                    events = trip.Events.OrderBy(e => e.CreatedAt).Select(e => new { e.EventType, e.Note, e.CreatedAt })
                };
            }
            return new
            {
                id = b.BookingId, b.BookingNumber, b.Status, b.PickupDate, b.PickupSlot, b.WeightKg, b.GoodsValue, b.GoodsDescription, b.SpecialInstructions,
                goods = b.GoodsCategory.Name, vehicleType = b.VehicleType.Name,
                pickup = new { city = b.PickupCity?.Name, address = b.PickupAddressText, contact = b.PickupContactName, phone = b.PickupContactPhone },
                drop = new { city = b.DropCity?.Name, address = b.DropAddressText, contact = b.DropContactName, phone = b.DropContactPhone },
                quote = quote is null ? null : new
                {
                    quote.DistanceKm, quote.VehicleCost, quote.DriverCost, quote.LoadingCharges, quote.TaxAmount, quote.TotalAmount,
                    quote.ValidUntil, quote.Status, expired = quote.Status == "Sent" && quote.ValidUntil < DateTime.UtcNow
                },
                payment, trip = tripView, invoice, b.CreatedAt, b.CancelledReason
            };
        });

        g.MapPost("/{id:long}/requote", async (long id, ClaimsPrincipal me, OlekarDbContext db, PricingService pricing, SettingsService settings) =>
        {
            var b = await OwnBooking(db, me, id);
            Guard.Require(b.Status == "Quoted", "Only unpaid bookings can be re-quoted.");
            var old = await db.Quotes.Where(q => q.BookingId == id && q.Status == "Sent").ToListAsync();
            old.ForEach(q => q.Status = q.ValidUntil < DateTime.UtcNow ? "Expired" : "Superseded");
            var from = await db.Cities.FindAsync(b.PickupCityId!.Value);
            var to = await db.Cities.FindAsync(b.DropCityId!.Value);
            var vt = await db.VehicleTypes.FindAsync(b.VehicleTypeId);
            var q = await NewQuote(vt!, PricingService.RoadKm(from!, to!), pricing, settings);
            q.BookingId = id;
            db.Quotes.Add(q);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Test payments only. Replace with the gateway flow: create order -> customer pays in checkout -> signed webhook marks Captured.
        g.MapPost("/{id:long}/pay", async (long id, PayRequest r, ClaimsPrincipal me, OlekarDbContext db, IConfiguration cfg, HttpContext http) =>
        {
            if (!string.Equals(cfg["Payments:Mode"], "Test", StringComparison.OrdinalIgnoreCase))
                throw new ApiException(501, "Online payment is not connected yet. Add your payment gateway keys to enable it.");
            var methods = new[] { "UPI", "CreditCard", "DebitCard", "NetBanking", "Wallet" };
            Guard.Require(methods.Contains(r.Method), "Choose a payment method.");
            var b = await OwnBooking(db, me, id);
            Guard.Require(b.Status == "Quoted", "This booking is already paid or closed.");
            var quote = await db.Quotes.Where(q => q.BookingId == id && q.Status == "Sent").OrderByDescending(q => q.CreatedAt).FirstOrDefaultAsync()
                ?? throw ApiException.BadRequest("Get a fresh quote first.");
            Guard.Require(quote.ValidUntil >= DateTime.UtcNow, "This quote has expired. Get a fresh quote first.");

            db.Payments.Add(new Payment
            {
                BookingId = id, CustomerId = b.CustomerId, QuoteId = quote.QuoteId, Amount = quote.TotalAmount, Method = r.Method,
                Gateway = "Test", GatewayOrderId = $"test_order_{Guid.NewGuid():N}"[..30], GatewayPaymentId = $"test_pay_{Guid.NewGuid():N}"[..30],
                Status = "Captured", PaidAt = DateTime.UtcNow
            });
            quote.Status = "Accepted"; quote.AcceptedAt = DateTime.UtcNow;
            b.Status = "Confirmed"; b.UpdatedAt = DateTime.UtcNow;
            Audit.Log(db, http, "Booking.Paid", "Booking", id, new { quote.TotalAmount, r.Method });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/{id:long}/cancel", async (long id, CancelRequest r, ClaimsPrincipal me, OlekarDbContext db, HttpContext http) =>
        {
            var b = await OwnBooking(db, me, id);
            Guard.Require(b.Status is "QuotePending" or "Quoted" or "Confirmed" or "Assigned", "Goods are already loaded, so this booking can't be cancelled online. Call support.");
            var trip = await db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).FirstOrDefaultAsync(t => t.BookingId == id && t.Status != "Cancelled");
            if (trip is not null)
            {
                Guard.Require(trip.Status is "Assigned" or "EnRouteToPickup" or "AtPickup", "Goods are already loaded, so this booking can't be cancelled online. Call support.");
                trip.Status = "Cancelled"; trip.UpdatedAt = DateTime.UtcNow;
                trip.Vehicle.AvailabilityStatus = "Available";
                trip.Driver.DutyStatus = "Available";
                db.TripEvents.Add(new TripEvent { TripId = trip.TripId, EventType = "Cancelled", Note = "Cancelled by customer", CreatedBy = me.UserId() });
            }
            foreach (var p in await db.Payments.Where(p => p.BookingId == id && p.Status == "Captured").ToListAsync())
                p.Status = "Refunded";   // test mode; real refunds go through the gateway and the Refunds table
            b.Status = "Cancelled"; b.CancelledBy = me.UserId(); b.CancelledReason = r.Reason; b.UpdatedAt = DateTime.UtcNow;
            Audit.Log(db, http, "Booking.Cancelled", "Booking", id, new { r.Reason });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    public static async Task<Quote> NewQuote(VehicleType vt, decimal km, PricingService pricing, SettingsService settings)
    {
        var p = await pricing.PriceAsync(vt, km);
        var minutes = await settings.GetIntAsync("QuoteValidityMinutes", 30);
        return new Quote
        {
            DistanceKm = p.DistanceKm, VehicleCost = p.VehicleCost, DriverCost = p.DriverCost, LoadingCharges = 0,
            PlatformFee = p.Commission, TaxAmount = p.TaxAmount, TotalAmount = p.TotalAmount, OwnerPayout = p.OwnerPayout,
            ValidUntil = DateTime.UtcNow.AddMinutes(minutes), Status = "Sent"
        };
    }

    private static async Task<long> CustomerId(OlekarDbContext db, ClaimsPrincipal me)
    {
        var uid = me.UserId();
        return await db.Customers.Where(c => c.UserId == uid).Select(c => (long?)c.CustomerId).FirstOrDefaultAsync()
            ?? throw ApiException.Forbidden("Customer profile not found.");
    }

    private static async Task<Booking> OwnBooking(OlekarDbContext db, ClaimsPrincipal me, long id)
    {
        var customerId = await CustomerId(db, me);
        return await db.Bookings.FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == customerId)
            ?? throw ApiException.NotFound("Booking not found.");
    }
}
