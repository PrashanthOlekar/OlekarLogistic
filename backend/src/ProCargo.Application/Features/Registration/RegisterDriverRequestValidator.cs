using FluentValidation;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Registration;

public sealed class RegisterDriverRequestValidator : AbstractValidator<RegisterDriverRequest>
{
    public RegisterDriverRequestValidator(TimeProvider clock)
    {
        RegistrationRules.FullName(RuleFor(request => request.FullName));
        RegistrationRules.Mobile(RuleFor(request => request.Mobile));
        RegistrationRules.Code(RuleFor(request => request.Code));
        RegistrationRules.Email(RuleFor(request => request.Email));

        RuleFor(request => request.LicenceNumber)
            .Must(number => IndianFormats.CleanLicence(number).Length is >= 10 and <= 20)
            .WithMessage("Enter the driving licence number as printed.");
        RuleFor(request => request.LicenceClass)
            .Must(licenceClass => LicenceClasses.All.Contains(licenceClass))
            .WithMessage("Choose the licence class.");
        RuleFor(request => request.LicenceExpiry)
            .Must(expiry => expiry > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            .WithMessage("This licence has expired.");
        RuleFor(request => request.AadhaarLast4)
            .Must(digits => string.IsNullOrEmpty(digits) || IndianFormats.FourDigits().IsMatch(digits))
            .WithMessage("Enter only the last 4 digits of Aadhaar.");
        RuleFor(request => request.EmergencyContactName).MaximumLength(FieldLimits.ContactName);
        RuleFor(request => request.EmergencyContactPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || MobileNumber.IsValid(phone))
            .WithMessage("Enter a valid emergency contact number.");
        RuleFor(request => request.OwnerMobile)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || MobileNumber.IsValid(phone))
            .WithMessage("Enter the owner's 10-digit mobile number.");
    }
}
