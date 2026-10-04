using FluentValidation;

namespace ProCargo.Application.Features.Trips;

public sealed class AssignTripRequestValidator : AbstractValidator<AssignTripRequest>
{
    public AssignTripRequestValidator()
    {
        RuleFor(request => request.BookingId).GreaterThan(0).WithMessage("Choose the booking.");
        RuleFor(request => request.VehicleId).GreaterThan(0).WithMessage("Choose a truck.");
        RuleFor(request => request.DriverId).GreaterThan(0).WithMessage("Choose a driver.");
    }
}
