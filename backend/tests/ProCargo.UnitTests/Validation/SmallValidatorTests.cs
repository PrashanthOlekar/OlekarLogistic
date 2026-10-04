using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Trips;
using ProCargo.Application.Features.Vehicles;

namespace ProCargo.UnitTests.Validation;

public sealed class SmallValidatorTests
{
    [Fact]
    public void Rejecting_needs_a_reason_but_approving_does_not()
    {
        var validator = new VerificationRequestValidator();

        Assert.True(validator.Validate(new VerificationRequest(true, null)).IsValid);
        Assert.False(validator.Validate(new VerificationRequest(false, " ")).IsValid);
        Assert.True(validator.Validate(new VerificationRequest(false, "RC photo is blurred")).IsValid);
    }

    [Theory]
    [InlineData("Pickup", "1234", true)]
    [InlineData("Delivery", " 0042 ", true)]
    [InlineData("Pickup", "12345", false)]
    [InlineData("Pickup", "12a4", false)]
    [InlineData("Unload", "1234", false)]
    public void Handover_needs_a_kind_and_a_four_digit_code(string kind, string code, bool valid)
    {
        Assert.Equal(valid, new HandoverRequestValidator().Validate(new HandoverRequest(kind, code)).IsValid);
    }

    [Fact]
    public void Vehicle_update_needs_something_to_change_and_a_known_status()
    {
        var validator = new UpdateVehicleRequestValidator();

        Assert.False(validator.Validate(new UpdateVehicleRequest(null, null)).IsValid);
        Assert.False(validator.Validate(new UpdateVehicleRequest("Parked", null)).IsValid);
        Assert.False(validator.Validate(new UpdateVehicleRequest(null, 5, RemoveDriver: true)).IsValid);
        Assert.True(validator.Validate(new UpdateVehicleRequest("Maintenance", null)).IsValid);
        Assert.True(validator.Validate(new UpdateVehicleRequest(null, null, RemoveDriver: true)).IsValid);
    }

    [Theory]
    [InlineData("KA 01 AB 4521", true)]
    [InlineData("mh12k1234", true)]
    [InlineData("KA-01", false)]
    public void Vehicle_registration_must_look_like_an_indian_number(string registration, bool valid)
    {
        var request = new CreateVehicleRequest(registration, 4, 4000, null, null, null);

        Assert.Equal(valid, new CreateVehicleRequestValidator(TestClock.Default).Validate(request).IsValid);
    }
}
