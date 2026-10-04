using FluentValidation;
using ProCargo.Domain.Trips;

namespace ProCargo.Application.Features.Trips;

public sealed class HandoverRequestValidator : AbstractValidator<HandoverRequest>
{
    public HandoverRequestValidator()
    {
        RuleFor(request => request.Kind)
            .Must(kind => kind is HandoverKind.Pickup or HandoverKind.Delivery)
            .WithMessage("Kind must be Pickup or Delivery.");
        RuleFor(request => request.Code)
            .NotEmpty().WithMessage("Enter the 4-digit code.")
            .Must(code => (code ?? string.Empty).Trim() is { Length: 4 } digits && digits.All(char.IsDigit)).WithMessage("The code has 4 digits.");
    }
}
