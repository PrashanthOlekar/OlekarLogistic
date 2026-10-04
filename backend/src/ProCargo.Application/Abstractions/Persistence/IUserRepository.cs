using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Auth;
using ProCargo.Application.Features.Users;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken);

    Task<User?> GetByMobileAsync(string mobile, CancellationToken cancellationToken);

    Task RecordLoginAsync(long userId, CancellationToken cancellationToken);

    /// <summary>The user and their customer, owner or driver detail.</summary>
    Task<UserProfile?> GetProfileAsync(long userId, CancellationToken cancellationToken);

    /// <summary>Creates the first admin if there is none. Returns true when one was created.</summary>
    Task<bool> EnsureAdminAsync(string mobile, string fullName, CancellationToken cancellationToken);

    Task<PagedResult<UserListItem>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken);

    /// <summary>Returns false when the user doesn't exist. Blocking also revokes every refresh token.</summary>
    Task<bool> SetStatusAsync(long userId, string status, CancellationToken cancellationToken);
}
