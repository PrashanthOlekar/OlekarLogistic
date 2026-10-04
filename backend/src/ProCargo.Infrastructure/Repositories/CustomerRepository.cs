using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class CustomerRepository(StoredProcedureExecutor database) : ICustomerRepository
{
    public Task<Customer?> GetByUserIdAsync(long userId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Customer>(StoredProcedures.CustomerGetByUserId, new { UserId = userId }, cancellationToken);
}
