using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Settlements;

/// <summary>Filters for GET /settlements.</summary>
public sealed class SettlementQuery : ListQuery
{
    /// <summary>AwaitingPod, Approved, Released or OnHold.</summary>
    public string? Status { get; set; }
}
