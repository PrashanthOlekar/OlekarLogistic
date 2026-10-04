using FluentValidation;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Auth;

public sealed class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpRequestValidator()
    {
        RuleFor(request => request.Mobile).Must(MobileNumber.IsValid).WithMessage(MobileNumber.InvalidMessage);
        RuleFor(request => request.Purpose)
            .Must(purpose => purpose is OtpPurpose.Login or OtpPurpose.Signup)
            .WithMessage("Purpose must be Login or Signup.");
    }
}
