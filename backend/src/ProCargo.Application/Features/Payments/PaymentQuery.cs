using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Payments;

/// <summary>Filters for GET /payments.</summary>
public sealed class PaymentQuery : ListQuery
{
    /// <summary>Created, Captured, Failed or Refunded.</summary>
    public string? Status { get; set; }
}
