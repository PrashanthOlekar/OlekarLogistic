using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<Customer?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);
}
