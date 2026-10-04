using FluentValidation;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Users;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.Status)
            .Must(status => status is UserStatus.Active or UserStatus.Blocked)
            .WithMessage("Status must be Active or Blocked.");
    }
}
