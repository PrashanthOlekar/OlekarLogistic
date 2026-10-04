using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Payments;

/// <summary>Customer payments, for admins.</summary>
public interface IPaymentService
{
    Task<PagedResult<PaymentListItem>> GetPagedAsync(PaymentQuery query, CancellationToken cancellationToken);
}
