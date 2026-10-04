using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Users;

/// <summary>Account search and blocking, for admins.</summary>
public interface IUserService
{
    Task<PagedResult<UserListItem>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken);

    Task UpdateAsync(long userId, UpdateUserRequest request, CancellationToken cancellationToken);
}
