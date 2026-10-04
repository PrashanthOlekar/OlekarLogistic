using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Payments;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<PagedResult<PaymentListItem>> GetPagedAsync(PaymentQuery query, CancellationToken cancellationToken);
}
