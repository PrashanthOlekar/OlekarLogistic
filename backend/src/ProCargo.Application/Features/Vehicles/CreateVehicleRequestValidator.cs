using FluentValidation;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Vehicles;

public sealed class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator(TimeProvider clock)
    {
        RuleFor(request => request.RegistrationNumber)
            .Must(number => IndianFormats.VehicleRegistration().IsMatch(IndianFormats.CleanRegistration(number)))
            .WithMessage("Enter the registration number as on the RC, for example KA01AB4521.");
        RuleFor(request => request.VehicleTypeId).GreaterThan(0).WithMessage("Choose the vehicle type.");
        RuleFor(request => request.CapacityKg)
            .InclusiveBetween(1, FieldLimits.MaxVehicleCapacityKg)
            .WithMessage("Enter the vehicle's load capacity in kg.");
        RuleFor(request => request.MakeModel).MaximumLength(FieldLimits.MakeModel);
        RuleFor(request => request.ManufactureYear)
            .InclusiveBetween((short)1980, (short)(clock.GetUtcNow().Year + 1))
            .When(request => request.ManufactureYear is not null)
            .WithMessage("Enter the year the vehicle was made.");
    }
}
