using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Bookings;

public sealed class CancelBookingRequestValidator : AbstractValidator<CancelBookingRequest>
{
    public CancelBookingRequestValidator()
    {
        RuleFor(request => request.Reason).MaximumLength(FieldLimits.CancelReason);
    }
}
