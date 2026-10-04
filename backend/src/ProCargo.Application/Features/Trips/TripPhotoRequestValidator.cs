using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Trips;

public sealed class TripPhotoRequestValidator : AbstractValidator<TripPhotoRequest>
{
    public TripPhotoRequestValidator()
    {
        RuleFor(request => request.Kind)
            .Must(kind => kind is DocumentType.PickupPhoto or DocumentType.Pod)
            .WithMessage("Kind must be PickupPhoto or POD.");
        RuleFor(request => request.Latitude).InclusiveBetween(-90m, 90m).When(request => request.Latitude is not null);
        RuleFor(request => request.Longitude).InclusiveBetween(-180m, 180m).When(request => request.Longitude is not null);
    }
}
