using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Abstractions.Storage;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Profiles;
using ProCargo.Application.Features.Trips;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.UnitTests.Services;

public sealed class TripServiceTests
{
    private const long OwnerId = 3;
    private const long OtherOwnerId = 4;

    private readonly ITripRepository _trips = Substitute.For<ITripRepository>();
    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IDriverRepository _drivers = Substitute.For<IDriverRepository>();
    private readonly IOwnerRepository _owners = Substitute.For<IOwnerRepository>();
    private readonly IPersonalDataProtector _protector = Substitute.For<IPersonalDataProtector>();
    private readonly ICurrentProfiles _profiles = Substitute.For<ICurrentProfiles>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly Booking _paidBooking = new() { BookingId = 1, Status = BookingStatus.Confirmed, VehicleTypeId = 4, WeightKg = 3000 };
    private readonly Vehicle _myTruck = new()
    {
        VehicleId = 20,
        OwnerId = OwnerId,
        VehicleTypeId = 4,
        CapacityKg = 4000,
        VerificationStatus = VehicleVerification.Approved,
        AvailabilityStatus = VehicleAvailability.Available,
        RegistrationNumber = "KA01AB4521",
    };
    private readonly Driver _myDriver = new()
    {
        DriverId = 30,
        OwnerId = OwnerId,
        KycStatus = KycStatus.Approved,
        LicenceExpiry = new DateOnly(2030, 1, 1),
    };

    public TripServiceTests()
    {
        _currentUser.Role.Returns(Roles.Owner);
        _currentUser.UserId.Returns(100);
        _profiles.GetOwnerAsync(Arg.Any<CancellationToken>())
            .Returns(new Owner { OwnerId = OwnerId, KycStatus = KycStatus.Approved });
        _owners.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => new Owner { OwnerId = call.Arg<long>(), KycStatus = KycStatus.Approved });

        _bookings.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(_paidBooking);
        _vehicles.GetByIdAsync(20, Arg.Any<CancellationToken>()).Returns(_myTruck);
        _drivers.GetByIdAsync(30, Arg.Any<CancellationToken>()).Returns(_myDriver);
        _protector.Protect(Arg.Any<string>()).Returns(call => "enc:" + call.Arg<string>());
        _protector.Unprotect(Arg.Any<string>()).Returns(call => call.Arg<string>().Replace("enc:", string.Empty));
        _trips.AssignAsync(Arg.Any<TripAssignment>(), Arg.Any<CancellationToken>())
            .Returns(new CreatedTrip { Id = 9, TripNumber = "TRP-24001" });
    }

    private TripService CreateService() => new(
        _trips,
        _bookings,
        _vehicles,
        _drivers,
        _owners,
        Substitute.For<IFileStorage>(),
        _protector,
        _profiles,
        _currentUser,
        Substitute.For<IAuditTrail>(),
        TestClock.Default,
        new AssignTripRequestValidator(),
        new TripEventRequestValidator(),
        new HandoverRequestValidator(),
        new TripPhotoRequestValidator(),
        NullLogger<TripService>.Instance);

    private static readonly AssignTripRequest TakeLoad = new(BookingId: 1, VehicleId: 20, DriverId: 30);

    [Fact]
    public async Task Owner_takes_a_paid_load_with_their_own_truck()
    {
        CreatedTrip trip = await CreateService().AssignAsync(TakeLoad, CancellationToken.None);

        Assert.Equal("TRP-24001", trip.TripNumber);
        await _trips.Received(1).AssignAsync(
            Arg.Is<TripAssignment>(assignment =>
                assignment.AcceptedByOwnerId == OwnerId
                && assignment.PickupOtpProtected.StartsWith("enc:")
                && assignment.DeliveryOtpProtected.StartsWith("enc:")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Owner_cannot_use_someone_elses_truck()
    {
        _myTruck.OwnerId = OtherOwnerId;

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService().AssignAsync(TakeLoad, CancellationToken.None));
    }

    [Fact]
    public async Task A_load_already_taken_is_a_conflict()
    {
        _paidBooking.Status = BookingStatus.Assigned;

        await Assert.ThrowsAsync<ConflictException>(() => CreateService().AssignAsync(TakeLoad, CancellationToken.None));
    }

    [Theory]
    [InlineData("busy-driver", "This driver is already on a trip.")]
    [InlineData("expired-licence", "This driver's licence has expired.")]
    [InlineData("small-truck", "The load is heavier than this vehicle's capacity.")]
    [InlineData("unverified-truck", "This vehicle is not verified yet.")]
    public async Task Assignment_rules_give_clear_reasons(string problem, string message)
    {
        switch (problem)
        {
            case "busy-driver":
                _drivers.HasActiveTripAsync(30, Arg.Any<CancellationToken>()).Returns(true);
                break;
            case "expired-licence":
                _myDriver.LicenceExpiry = new DateOnly(2026, 10, 1);
                break;
            case "small-truck":
                _myTruck.CapacityKg = 2000;
                break;
            case "unverified-truck":
                _myTruck.VerificationStatus = VehicleVerification.Pending;
                break;
        }

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().AssignAsync(TakeLoad, CancellationToken.None));

        Assert.Equal(message, error.Message);
        await _trips.DidNotReceive().AssignAsync(Arg.Any<TripAssignment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Owner_without_approved_kyc_cannot_take_loads()
    {
        _profiles.GetOwnerAsync(Arg.Any<CancellationToken>()).Returns(new Owner { OwnerId = OwnerId, KycStatus = KycStatus.Pending });

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().AssignAsync(TakeLoad, CancellationToken.None));
    }

    private void GivenDriverTrip(string status, string pickupCode = "1111", string deliveryCode = "2222")
    {
        _currentUser.Role.Returns(Roles.Driver);
        _profiles.GetDriverAsync(Arg.Any<CancellationToken>()).Returns(_myDriver);
        _trips.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns(new Trip
        {
            TripId = 9,
            DriverId = _myDriver.DriverId,
            Status = status,
            PickupOtpProtected = "enc:" + pickupCode,
            DeliveryOtpProtected = "enc:" + deliveryCode,
        });
    }

    [Fact]
    public async Task Driver_cannot_skip_steps()
    {
        GivenDriverTrip(TripStatus.Assigned);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().RecordEventAsync(9, new TripEventRequest("StartTrip", null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Correct_pickup_code_loads_the_goods()
    {
        GivenDriverTrip(TripStatus.AtPickup);

        await CreateService().ConfirmHandoverAsync(9, new HandoverRequest("Pickup", "1111"), CancellationToken.None);

        await _trips.Received(1).ChangeStatusAsync(
            Arg.Is<TripStatusChange>(change => change.NewStatus == TripStatus.Loaded && change.EventType == TripEventType.PickupOtpVerified),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Wrong_pickup_code_is_refused()
    {
        GivenDriverTrip(TripStatus.AtPickup);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().ConfirmHandoverAsync(9, new HandoverRequest("Pickup", "9999"), CancellationToken.None));

        Assert.Equal("That code doesn't match. Ask the sender to check it.", error.Message);
    }

    [Fact]
    public async Task Delivery_needs_the_pod_before_the_code()
    {
        GivenDriverTrip(TripStatus.AtDestination);
        _trips.HasPodAsync(9, Arg.Any<CancellationToken>()).Returns(false);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().ConfirmHandoverAsync(9, new HandoverRequest("Delivery", "2222"), CancellationToken.None));

        Assert.Equal("Upload the signed delivery receipt (POD) first.", error.Message);
        await _trips.DidNotReceive().CompleteDeliveryAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Another_drivers_trip_is_not_found()
    {
        GivenDriverTrip(TripStatus.Assigned);
        _trips.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns(new Trip { TripId = 9, DriverId = 999, Status = TripStatus.Assigned });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().RecordEventAsync(9, new TripEventRequest("EnRoute", null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Pod_approval_issues_an_igst_invoice_for_an_interstate_trip()
    {
        _currentUser.Role.Returns(Roles.Admin);
        _trips.GetForPodApprovalAsync(9, Arg.Any<CancellationToken>()).Returns(new PodApprovalData
        {
            TripId = 9,
            Status = TripStatus.Delivered,
            PickupState = "Karnataka",
            DropState = "Maharashtra",
            TaxAmount = 1000,
            TotalAmount = 21000,
        });
        _trips.NextInvoiceSequenceAsync(Arg.Any<CancellationToken>()).Returns(412);

        await CreateService().ApprovePodAsync(9, CancellationToken.None);

        await _trips.Received(1).ApprovePodAsync(
            Arg.Is<PodApproval>(approval =>
                approval.InvoiceNumber == "PC/26-27/000412"
                && approval.Igst == 1000 && approval.Cgst == 0
                && approval.TaxableAmount == 20000
                && approval.PlaceOfSupply == "Karnataka"),
            Arg.Any<CancellationToken>());
    }
}
