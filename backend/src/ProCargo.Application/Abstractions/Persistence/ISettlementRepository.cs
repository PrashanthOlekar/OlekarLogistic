using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Settlements;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

public interface ISettlementRepository
{
    Task<Settlement?> GetByIdAsync(long settlementId, CancellationToken cancellationToken);

    /// <param name="ownerId">Only this owner's payouts; null for everyone (admin).</param>
    Task<PagedResult<SettlementListItem>> GetPagedAsync(SettlementQuery query, long? ownerId, CancellationToken cancellationToken);

    /// <summary>Marks an Approved payout as Released. Conflict if it isn't Approved.</summary>
    Task RecordPayoutAsync(long settlementId, string utr, long releasedBy, CancellationToken cancellationToken);
}
