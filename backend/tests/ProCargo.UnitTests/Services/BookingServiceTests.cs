using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Bookings;
using ProCargo.Application.Features.Pricing;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.UnitTests.Services;

public sealed class BookingServiceTests
{
    private const long MyCustomerId = 10;
    private const long BookingId = 500;

    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IReferenceDataRepository _referenceData = Substitute.For<IReferenceDataRepository>();
    private readonly ICurrentProfiles _profiles = Substitute.For<ICurrentProfiles>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IAuditTrail _audit = Substitute.For<IAuditTrail>();
    private PaymentOptions _payments = new() { Mode = "Test" };

    public BookingServiceTests()
    {
        _profiles.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(MyCustomerId);
        _currentUser.Role.Returns(Roles.Customer);
        _currentUser.UserId.Returns(1);
    }

    private BookingService CreateService() => new(
        _bookings,
        _referenceData,
        Substitute.For<IQuotePricer>(),
        _profiles,
        _currentUser,
        Substitute.For<IPersonalDataProtector>(),
        _audit,
        Options.Create(_payments),
        TestClock.Default,
        new CreateBookingRequestValidator(TestClock.Default),
        new PayBookingRequestValidator(),
        new CancelBookingRequestValidator(),
        NullLogger<BookingService>.Instance);

    private void GivenBooking(string status, long customerId = MyCustomerId) =>
        _bookings.GetByIdAsync(BookingId, Arg.Any<CancellationToken>())
            .Returns(new Booking { BookingId = BookingId, CustomerId = customerId, Status = status, VehicleTypeId = 4, WeightKg = 1000 });

    private void GivenOpenQuote(DateTime validUntil) =>
        _bookings.GetLatestOpenQuoteAsync(BookingId, Arg.Any<CancellationToken>())
            .Returns(new Quote { QuoteId = 77, BookingId = BookingId, TotalAmount = 18270, ValidUntil = validUntil, Status = QuoteStatus.Sent });

    [Fact]
    public async Task Someone_elses_booking_is_not_found()
    {
        GivenBooking(BookingStatus.Quoted, customerId: 999);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().PayAsync(BookingId, new PayBookingRequest("UPI"), CancellationToken.None));
    }

    [Fact]
    public async Task An_expired_quote_cannot_be_paid()
    {
        GivenBooking(BookingStatus.Quoted);
        GivenOpenQuote(TestClock.Default.GetUtcNow().UtcDateTime.AddMinutes(-1));

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().PayAsync(BookingId, new PayBookingRequest("UPI"), CancellationToken.None));

        Assert.Equal("This quote has expired. Get a fresh quote first.", error.Message);
        await _bookings.DidNotReceive().PayAsync(Arg.Any<PaymentRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_paid_booking_cannot_be_paid_again()
    {
        GivenBooking(BookingStatus.Confirmed);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().PayAsync(BookingId, new PayBookingRequest("UPI"), CancellationToken.None));
    }

    [Fact]
    public async Task Paying_a_valid_quote_records_a_test_payment_and_an_audit_entry()
    {
        GivenBooking(BookingStatus.Quoted);
        GivenOpenQuote(TestClock.Default.GetUtcNow().UtcDateTime.AddMinutes(20));
        _bookings.PayAsync(Arg.Any<PaymentRecord>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentReceipt { PaymentId = 1, Amount = 18270 });

        PaymentReceipt receipt = await CreateService().PayAsync(BookingId, new PayBookingRequest("UPI"), CancellationToken.None);

        Assert.Equal(18270, receipt.Amount);
        await _bookings.Received(1).PayAsync(
            Arg.Is<PaymentRecord>(payment => payment.QuoteId == 77 && payment.Gateway == "Test" && payment.Method == "UPI"),
            Arg.Any<CancellationToken>());
        await _audit.Received(1).RecordAsync("Booking.Paid", "Booking", BookingId, Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Live_payment_mode_is_not_available_yet()
    {
        _payments = new PaymentOptions { Mode = "Live" };

        await Assert.ThrowsAsync<FeatureUnavailableException>(() =>
            CreateService().PayAsync(BookingId, new PayBookingRequest("UPI"), CancellationToken.None));
    }

    [Theory]
    [InlineData(BookingStatus.InTransit)]
    [InlineData(BookingStatus.Delivered)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Bookings_cannot_be_cancelled_once_loaded(string status)
    {
        GivenBooking(status);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().CancelAsync(BookingId, new CancelBookingRequest(null), CancellationToken.None));
        await _bookings.DidNotReceive().CancelAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admins_list_every_booking_and_customers_only_their_own()
    {
        var query = new BookingQuery();

        await CreateService().GetPagedAsync(query, CancellationToken.None);
        await _bookings.Received(1).GetPagedAsync(query, MyCustomerId, Arg.Any<CancellationToken>());

        _currentUser.IsInRole(Roles.Admin).Returns(true);
        await CreateService().GetPagedAsync(query, CancellationToken.None);
        await _bookings.Received(1).GetPagedAsync(query, null, Arg.Any<CancellationToken>());
    }
}
