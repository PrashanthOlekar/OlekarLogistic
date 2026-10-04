using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Registration;

public sealed class RegisterCustomerRequestValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerRequestValidator()
    {
        RegistrationRules.FullName(RuleFor(request => request.FullName));
        RegistrationRules.Mobile(RuleFor(request => request.Mobile));
        RegistrationRules.Code(RuleFor(request => request.Code));
        RegistrationRules.Email(RuleFor(request => request.Email));

        RuleFor(request => request.CompanyName).MaximumLength(FieldLimits.CompanyName);
        RuleFor(request => request.Gstin)
            .Must(gstin => string.IsNullOrWhiteSpace(gstin) || gstin.Trim().Length == 15)
            .WithMessage("GSTIN has 15 characters.");
    }
}
