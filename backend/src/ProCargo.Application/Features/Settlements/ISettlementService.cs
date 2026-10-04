using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Settlements;

/// <summary>Owner payouts.</summary>
public interface ISettlementService
{
    /// <summary>Owners see their own payouts; admins see all.</summary>
    Task<PagedResult<SettlementListItem>> GetPagedAsync(SettlementQuery query, CancellationToken cancellationToken);

    /// <summary>Admin: record a payout sent from the bank.</summary>
    Task RecordPayoutAsync(long settlementId, RecordPayoutRequest request, CancellationToken cancellationToken);
}
