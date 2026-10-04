using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Auth;
using ProCargo.Application.Features.Users;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class UserRepository(StoredProcedureExecutor database) : IUserRepository
{
    public Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<User>(StoredProcedures.UserGetById, new { UserId = userId }, cancellationToken);

    public Task<User?> GetByMobileAsync(string mobile, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<User>(StoredProcedures.UserGetByMobile, new { Mobile = mobile }, cancellationToken);

    public Task RecordLoginAsync(long userId, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.UserRecordLogin, new { UserId = userId }, cancellationToken);

    public Task<UserProfile?> GetProfileAsync(long userId, CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(StoredProcedures.UserGetProfile, new { UserId = userId }, async grid =>
        {
            UserProfile? profile = await grid.ReadFirstOrDefaultAsync<UserProfile>();
            CustomerProfile? customer = await grid.ReadFirstOrDefaultAsync<CustomerProfile>();
            OwnerProfile? owner = await grid.ReadFirstOrDefaultAsync<OwnerProfile>();
            DriverProfile? driver = await grid.ReadFirstOrDefaultAsync<DriverProfile>();

            if (profile is not null)
            {
                profile.Detail = profile.Role switch
                {
                    Roles.Customer => customer,
                    Roles.Owner => owner,
                    Roles.Driver => driver,
                    _ => null,
                };
            }

            return profile;
        }, cancellationToken);

    public Task<bool> EnsureAdminAsync(string mobile, string fullName, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<bool>(StoredProcedures.UserEnsureAdmin, new { Mobile = mobile, FullName = fullName }, cancellationToken);

    public Task<PagedResult<UserListItem>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Role", query.Role, DbType.String);
        parameters.Add("Status", query.Status, DbType.String);
        parameters.Add("Search", query.CleanSearch, DbType.String);
        parameters.Add("Sort", query.Sort ?? "Newest", DbType.String);

        return database.QueryPagedAsync<UserListItem>(StoredProcedures.UserGetPaged, parameters, query.ToPage(), cancellationToken);
    }

    public async Task<bool> SetStatusAsync(long userId, string status, CancellationToken cancellationToken)
    {
        int rows = await database.ExecuteScalarAsync<int>(StoredProcedures.UserSetStatus, new { UserId = userId, Status = status }, cancellationToken);
        return rows > 0;
    }
}
