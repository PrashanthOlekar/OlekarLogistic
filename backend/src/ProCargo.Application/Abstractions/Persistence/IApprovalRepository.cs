using ProCargo.Application.Features.Approvals;

namespace ProCargo.Application.Abstractions.Persistence;

public interface IApprovalRepository
{
    /// <summary>Owners, drivers and vehicles waiting for a KYC decision, with their documents.</summary>
    Task<PendingApprovals> GetPendingAsync(CancellationToken cancellationToken);
}
