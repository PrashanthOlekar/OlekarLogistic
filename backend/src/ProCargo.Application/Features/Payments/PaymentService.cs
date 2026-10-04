using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Payments;

internal sealed class PaymentService(IPaymentRepository payments) : IPaymentService
{
    public Task<PagedResult<PaymentListItem>> GetPagedAsync(PaymentQuery query, CancellationToken cancellationToken) =>
        payments.GetPagedAsync(query, cancellationToken);
}
