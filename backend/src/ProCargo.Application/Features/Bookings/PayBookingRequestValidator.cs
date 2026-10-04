using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Bookings;

public sealed class PayBookingRequestValidator : AbstractValidator<PayBookingRequest>
{
    public PayBookingRequestValidator()
    {
        RuleFor(request => request.Method)
            .Must(method => PaymentMethods.All.Contains(method))
            .WithMessage("Choose a payment method.");
    }
}
