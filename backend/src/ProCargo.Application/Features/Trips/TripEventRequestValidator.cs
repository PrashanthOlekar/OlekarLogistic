using FluentValidation;
using ProCargo.Domain.Trips;

namespace ProCargo.Application.Features.Trips;

public sealed class TripEventRequestValidator : AbstractValidator<TripEventRequest>
{
    public TripEventRequestValidator()
    {
        RuleFor(request => request.Action)
            .Must(action => TripSteps.Find(action) is not null)
            .WithMessage("Unknown action.");
        RuleFor(request => request.Latitude).InclusiveBetween(-90m, 90m).When(request => request.Latitude is not null);
        RuleFor(request => request.Longitude).InclusiveBetween(-180m, 180m).When(request => request.Longitude is not null);
    }
}
