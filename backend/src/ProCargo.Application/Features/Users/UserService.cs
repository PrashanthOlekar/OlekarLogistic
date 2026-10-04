using FluentValidation;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Users;

internal sealed class UserService(
    IUserRepository users,
    IAuditTrail auditTrail,
    ICurrentUser currentUser,
    IValidator<UpdateUserRequest> validator,
    ILogger<UserService> logger) : IUserService
{
    public Task<PagedResult<UserListItem>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken) =>
        users.GetPagedAsync(query, cancellationToken);

    public async Task UpdateAsync(long userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        if (userId == currentUser.UserId)
        {
            throw new BusinessRuleException("You can't block your own account.");
        }

        bool found = await users.SetStatusAsync(userId, request.Status, cancellationToken);
        if (!found)
        {
            throw new NotFoundException("User not found.");
        }

        bool blocked = request.Status == UserStatus.Blocked;
        await auditTrail.RecordAsync(blocked ? "User.Blocked" : "User.Unblocked", "User", userId, null, cancellationToken);
        logger.LogInformation("User {UserId} set to {Status} by {AdminId}", userId, request.Status, currentUser.UserId);
    }
}
