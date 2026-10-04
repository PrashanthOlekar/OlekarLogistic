using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Owners;

public interface IOwnerService
{
    /// <summary>The signed-in owner's overview.</summary>
    Task<OwnerDashboard> GetMyDashboardAsync(CancellationToken cancellationToken);

    /// <summary>Admin: approve or reject an owner's KYC.</summary>
    Task SetVerificationAsync(long ownerId, VerificationRequest request, CancellationToken cancellationToken);
}
