using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Documents;

public sealed class UploadDocumentRequestValidator : AbstractValidator<UploadDocumentRequest>
{
    public UploadDocumentRequestValidator()
    {
        RuleFor(request => request.DocType)
            .Must((request, docType) => DocumentType.IsAllowed(request.EntityType, docType))
            .WithMessage("Choose a valid document type.");
        RuleFor(request => request.EntityId)
            .NotNull().When(request => request.EntityType == DocumentEntity.Vehicle)
            .WithMessage("Choose the vehicle.");
        RuleFor(request => request.DocumentNumber).MaximumLength(FieldLimits.DocumentNumber);
    }
}
