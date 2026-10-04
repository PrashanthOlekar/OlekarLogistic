using ProCargo.Application.Abstractions.Persistence;

namespace ProCargo.Application.Features.Approvals;

internal sealed class ApprovalService(IApprovalRepository approvals) : IApprovalService
{
    public Task<PendingApprovals> GetPendingAsync(CancellationToken cancellationToken) =>
        approvals.GetPendingAsync(cancellationToken);
}
