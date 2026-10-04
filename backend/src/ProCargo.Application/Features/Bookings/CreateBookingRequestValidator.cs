using FluentValidation;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Bookings;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator(TimeProvider clock)
    {
        RuleFor(request => request.PickupCityId).GreaterThan(0).WithMessage("Choose a pickup city.");
        RuleFor(request => request.DropCityId).GreaterThan(0).WithMessage("Choose a delivery city.");
        RuleFor(request => request.VehicleTypeId).GreaterThan(0).WithMessage("Choose a vehicle type.");
        RuleFor(request => request.GoodsCategoryId).GreaterThan(0).WithMessage("Choose what you are sending.");

        RuleFor(request => request.PickupAddress)
            .NotEmpty().WithMessage("Enter both the pickup and delivery addresses.")
            .MaximumLength(FieldLimits.Address);
        RuleFor(request => request.DropAddress)
            .NotEmpty().WithMessage("Enter both the pickup and delivery addresses.")
            .MaximumLength(FieldLimits.Address);

        RuleFor(request => request.PickupContactName).MaximumLength(FieldLimits.ContactName);
        RuleFor(request => request.DropContactName).MaximumLength(FieldLimits.ContactName);
        RuleFor(request => request.PickupContactPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || MobileNumber.IsValid(phone))
            .WithMessage(MobileNumber.InvalidMessage);
        RuleFor(request => request.DropContactPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || MobileNumber.IsValid(phone))
            .WithMessage(MobileNumber.InvalidMessage);

        RuleFor(request => request.GoodsDescription)
            .NotEmpty().WithMessage("Describe the goods, for example \"40 cartons of biscuits\".")
            .MaximumLength(FieldLimits.GoodsDescription);
        RuleFor(request => request.WeightKg).GreaterThan(0).WithMessage("Enter the approximate weight in kg.");
        RuleFor(request => request.GoodsValue)
            .GreaterThanOrEqualTo(0).When(request => request.GoodsValue is not null)
            .WithMessage("The value of the goods can't be negative.");

        RuleFor(request => request.PickupDate)
            .Must(date => date >= IndianTime.TodayFor(clock.GetUtcNow().UtcDateTime))
            .WithMessage("The pickup date can't be in the past.");
        RuleFor(request => request.PickupSlot).MaximumLength(FieldLimits.PickupSlot);
        RuleFor(request => request.SpecialInstructions).MaximumLength(FieldLimits.SpecialInstructions);
    }
}
