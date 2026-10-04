using FluentValidation;

namespace ProCargo.Application.Features.Pricing;

public sealed class PriceEstimateRequestValidator : AbstractValidator<PriceEstimateRequest>
{
    public PriceEstimateRequestValidator()
    {
        RuleFor(request => request.PickupCityId).GreaterThan(0).WithMessage("Choose a pickup city.");
        RuleFor(request => request.DropCityId).GreaterThan(0).WithMessage("Choose a delivery city.");
        RuleFor(request => request.VehicleTypeId).GreaterThan(0).WithMessage("Choose a vehicle type.");
        RuleFor(request => request.WeightKg).GreaterThan(0).WithMessage("Enter the approximate weight in kg.");
    }
}
