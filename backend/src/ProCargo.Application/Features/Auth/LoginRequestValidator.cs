using FluentValidation;
using ProCargo.Domain.Common;

namespace ProCargo.Application.Features.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Mobile).Must(MobileNumber.IsValid).WithMessage(MobileNumber.InvalidMessage);
        RuleFor(request => request.Code)
            .NotEmpty().WithMessage("Enter the code we sent you.")
            .Matches("^[0-9]{6}$").WithMessage("The code has 6 digits.");
    }
}
