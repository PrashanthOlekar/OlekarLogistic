using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Common;

public sealed class VerificationRequestValidator : AbstractValidator<VerificationRequest>
{
    public VerificationRequestValidator()
    {
        RuleFor(request => request.Reason)
            .Must((request, reason) => request.Approve || !string.IsNullOrWhiteSpace(reason))
            .WithMessage("Give a reason so the applicant knows what to fix.")
            .MaximumLength(FieldLimits.RejectionReason);
    }
}
