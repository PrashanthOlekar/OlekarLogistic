using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Vehicles;

public sealed class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(request => request.AvailabilityStatus)
            .Must(status => status is null || VehicleAvailability.All.Contains(status))
            .WithMessage("Choose Available, Busy or Maintenance.");

        RuleFor(request => request)
            .Must(request => request.AvailabilityStatus is not null || request.CurrentDriverId is not null || request.RemoveDriver)
            .WithName("request")
            .WithMessage("Nothing to change.");

        RuleFor(request => request.CurrentDriverId)
            .Null()
            .When(request => request.RemoveDriver)
            .WithMessage("Send either a driver or removeDriver, not both.");
    }
}
