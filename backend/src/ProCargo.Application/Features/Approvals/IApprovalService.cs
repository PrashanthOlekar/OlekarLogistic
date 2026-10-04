namespace ProCargo.Application.Features.Approvals;

public interface IApprovalService
{
    Task<PendingApprovals> GetPendingAsync(CancellationToken cancellationToken);
}
