using FluentValidation;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Registration;

public sealed class RegisterOwnerRequestValidator : AbstractValidator<RegisterOwnerRequest>
{
    public RegisterOwnerRequestValidator()
    {
        RegistrationRules.FullName(RuleFor(request => request.FullName));
        RegistrationRules.Mobile(RuleFor(request => request.Mobile));
        RegistrationRules.Code(RuleFor(request => request.Code));
        RegistrationRules.Email(RuleFor(request => request.Email));

        RuleFor(request => request.BusinessName).MaximumLength(FieldLimits.BusinessName);
        RuleFor(request => request.Pan)
            .Must(pan => IndianFormats.Pan().IsMatch(IndianFormats.CleanUpper(pan)))
            .WithMessage("Enter a valid PAN, for example ABCDE1234F.");
        RuleFor(request => request.AadhaarLast4)
            .Must(digits => IndianFormats.FourDigits().IsMatch(digits ?? string.Empty))
            .WithMessage("Enter only the last 4 digits of Aadhaar.");
        RuleFor(request => request.AccountNumber)
            .Must(number => IndianFormats.CleanAccountNumber(number).Length is >= 9 and <= 18)
            .WithMessage("Enter a valid bank account number.");
        RuleFor(request => request.Ifsc)
            .Must(ifsc => IndianFormats.Ifsc().IsMatch(IndianFormats.CleanUpper(ifsc)))
            .WithMessage("Enter a valid IFSC, for example HDFC0001234.");
        RuleFor(request => request.AccountHolder)
            .NotEmpty().WithMessage("Enter the account holder's name.")
            .MaximumLength(FieldLimits.FullName);
        RuleFor(request => request.BankName).MaximumLength(FieldLimits.BankName);
    }
}
