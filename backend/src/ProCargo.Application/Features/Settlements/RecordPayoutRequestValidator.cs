using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Settlements;

public sealed class RecordPayoutRequestValidator : AbstractValidator<RecordPayoutRequest>
{
    public RecordPayoutRequestValidator()
    {
        RuleFor(request => request.Utr)
            .NotEmpty().WithMessage("Enter the bank transfer reference (UTR).")
            .MaximumLength(FieldLimits.Utr);
    }
}
