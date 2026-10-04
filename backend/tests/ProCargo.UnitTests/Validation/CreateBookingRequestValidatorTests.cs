using FluentValidation.Results;
using ProCargo.Application.Features.Bookings;

namespace ProCargo.UnitTests.Validation;

public sealed class CreateBookingRequestValidatorTests
{
    private readonly CreateBookingRequestValidator _validator = new(TestClock.Default);

    private static CreateBookingRequest ValidRequest() => new(
        PickupCityId: 1,
        PickupAddress: "Plot 12, Peenya Industrial Area",
        PickupContactName: "Ravi",
        PickupContactPhone: "9845012345",
        DropCityId: 6,
        DropAddress: "Gokul Road, Hubballi",
        DropContactName: null,
        DropContactPhone: null,
        GoodsCategoryId: 5,
        GoodsDescription: "40 cartons of biscuits",
        WeightKg: 1200,
        GoodsValue: 85000,
        VehicleTypeId: 4,
        PickupDate: new DateOnly(2026, 10, 6),
        PickupSlot: "Morning",
        SpecialInstructions: null);

    [Fact]
    public void A_complete_booking_is_valid()
    {
        Assert.True(_validator.Validate(ValidRequest()).IsValid);
    }

    [Fact]
    public void Today_in_india_is_allowed_but_yesterday_is_not()
    {
        Assert.True(_validator.Validate(ValidRequest() with { PickupDate = new DateOnly(2026, 10, 4) }).IsValid);

        ValidationResult result = _validator.Validate(ValidRequest() with { PickupDate = new DateOnly(2026, 10, 3) });
        Assert.Contains(result.Errors, error => error.ErrorMessage == "The pickup date can't be in the past.");
    }

    [Fact]
    public void Missing_fields_report_friendly_messages()
    {
        ValidationResult result = _validator.Validate(ValidRequest() with
        {
            PickupAddress = "",
            GoodsDescription = " ",
            WeightKg = 0,
            PickupContactPhone = "123",
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBookingRequest.PickupAddress));
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Enter the approximate weight in kg.");
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBookingRequest.GoodsDescription));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBookingRequest.PickupContactPhone));
    }
}
