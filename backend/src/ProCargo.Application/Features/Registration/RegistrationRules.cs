using FluentValidation;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Registration;

/// <summary>Rules shared by the three registration validators.</summary>
internal static class RegistrationRules
{
    public static void FullName<T>(IRuleBuilder<T, string> rule) => rule
        .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2).WithMessage("Enter your full name.")
        .MaximumLength(FieldLimits.FullName);

    public static void Mobile<T>(IRuleBuilder<T, string> rule) => rule
        .Must(MobileNumber.IsValid).WithMessage(MobileNumber.InvalidMessage);

    public static void Code<T>(IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Enter the code we sent you.");

    public static void Email<T>(IRuleBuilder<T, string?> rule) => rule
        .EmailAddress().WithMessage("Enter a valid email address.")
        .MaximumLength(FieldLimits.Email);
}
