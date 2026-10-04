using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Bookings;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class BookingRepository(StoredProcedureExecutor database) : IBookingRepository
{
    public async Task<CreatedBooking> CreateAsync(NewBooking booking, Quote quote, CancellationToken cancellationToken)
    {
        object parameters = new
        {
            booking.CustomerId,
            booking.PickupCityId,
            booking.PickupAddressText,
            booking.PickupLatitude,
            booking.PickupLongitude,
            booking.PickupContactName,
            booking.PickupContactPhone,
            booking.DropCityId,
            booking.DropAddressText,
            booking.DropLatitude,
            booking.DropLongitude,
            booking.DropContactName,
            booking.DropContactPhone,
            booking.GoodsCategoryId,
            booking.GoodsDescription,
            booking.WeightKg,
            booking.GoodsValue,
            booking.VehicleTypeId,
            booking.PickupDate,
            booking.PickupSlot,
            booking.SpecialInstructions,
            quote.DistanceKm,
            quote.VehicleCost,
            quote.DriverCost,
            quote.PlatformFee,
            quote.TaxAmount,
            quote.TotalAmount,
            quote.OwnerPayout,
            quote.ValidUntil,
        };

        return await database.QuerySingleOrDefaultAsync<CreatedBooking>(StoredProcedures.BookingCreate, parameters, cancellationToken)
            ?? throw new InvalidOperationException("usp_Booking_Create returned no row.");
    }

    public Task<Booking?> GetByIdAsync(long bookingId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Booking>(StoredProcedures.BookingGetById, new { BookingId = bookingId }, cancellationToken);

    public Task<PagedResult<BookingListItem>> GetPagedAsync(BookingQuery query, long? customerId, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("CustomerId", customerId, DbType.Int64);
        parameters.Add("Status", query.Status, DbType.String);
        parameters.Add("Search", query.CleanSearch, DbType.String);
        parameters.Add("Sort", query.Sort ?? "Newest", DbType.String);

        return database.QueryPagedAsync<BookingListItem>(StoredProcedures.BookingGetPaged, parameters, query.ToPage(), cancellationToken);
    }

    public Task<BookingDetailData?> GetDetailAsync(long bookingId, CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(StoredProcedures.BookingGetDetail, new { BookingId = bookingId }, async grid =>
        {
            BookingDetailRow? booking = await grid.ReadFirstOrDefaultAsync<BookingDetailRow>();
            Quote? quote = await grid.ReadFirstOrDefaultAsync<Quote>();
            PaymentSummary? payment = await grid.ReadFirstOrDefaultAsync<PaymentSummary>();
            InvoiceSummary? invoice = await grid.ReadFirstOrDefaultAsync<InvoiceSummary>();
            BookingTripRow? trip = await grid.ReadFirstOrDefaultAsync<BookingTripRow>();
            List<TripEventItem> events = (await grid.ReadAsync<TripEventItem>()).AsList();

            return booking is null
                ? null
                : new BookingDetailData
                {
                    Booking = booking,
                    LatestQuote = quote,
                    Payment = payment,
                    Invoice = invoice,
                    Trip = trip,
                    Events = events,
                };
        }, cancellationToken);

    public Task<Quote?> GetLatestOpenQuoteAsync(long bookingId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Quote>(StoredProcedures.QuoteGetLatestOpen, new { BookingId = bookingId }, cancellationToken);

    public Task<long> RequoteAsync(long bookingId, Quote quote, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<long>(StoredProcedures.BookingRequote, new
        {
            BookingId = bookingId,
            quote.DistanceKm,
            quote.VehicleCost,
            quote.DriverCost,
            quote.PlatformFee,
            quote.TaxAmount,
            quote.TotalAmount,
            quote.OwnerPayout,
            quote.ValidUntil,
        }, cancellationToken);

    public async Task<PaymentReceipt> PayAsync(PaymentRecord payment, CancellationToken cancellationToken) =>
        await database.QuerySingleOrDefaultAsync<PaymentReceipt>(StoredProcedures.BookingPay, new
        {
            payment.BookingId,
            payment.QuoteId,
            payment.Method,
            payment.Gateway,
            payment.GatewayOrderId,
            payment.GatewayPaymentId,
        }, cancellationToken)
        ?? throw new InvalidOperationException("usp_Booking_Pay returned no row.");

    public Task CancelAsync(long bookingId, long cancelledBy, string? reason, CancellationToken cancellationToken) =>
        database.ExecuteAsync(
            StoredProcedures.BookingCancel,
            new { BookingId = bookingId, CancelledBy = cancelledBy, Reason = reason },
            cancellationToken);

    public Task<IReadOnlyList<AssignableVehicle>> GetAssignableVehiclesAsync(long bookingId, CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(StoredProcedures.BookingGetAssignableVehicles, new { BookingId = bookingId }, async grid =>
        {
            List<AssignableVehicleRow> vehicles = (await grid.ReadAsync<AssignableVehicleRow>()).AsList();
            ILookup<long, AssignableDriver> driversByOwner = (await grid.ReadAsync<AssignableDriverRow>())
                .ToLookup(driver => driver.OwnerId, driver => new AssignableDriver(driver.Id, driver.Name));

            return (IReadOnlyList<AssignableVehicle>)vehicles
                .Select(vehicle => new AssignableVehicle
                {
                    Id = vehicle.Id,
                    RegistrationNumber = vehicle.RegistrationNumber,
                    Owner = vehicle.Owner,
                    CurrentDriverId = vehicle.CurrentDriverId,
                    Drivers = driversByOwner[vehicle.OwnerId].ToList(),
                })
                .ToList();
        }, cancellationToken);

    private sealed class AssignableVehicleRow
    {
        public long Id { get; set; }

        public long OwnerId { get; set; }

        public string RegistrationNumber { get; set; } = string.Empty;

        public string Owner { get; set; } = string.Empty;

        public long? CurrentDriverId { get; set; }
    }

    private sealed class AssignableDriverRow
    {
        public long Id { get; set; }

        public long OwnerId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
