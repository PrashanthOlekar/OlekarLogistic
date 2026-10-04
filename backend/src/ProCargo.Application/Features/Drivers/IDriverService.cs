using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Drivers;

public interface IDriverService
{
    /// <summary>Owners see the drivers linked to them; admins see everyone.</summary>
    Task<PagedResult<DriverListItem>> GetPagedAsync(DriverQuery query, CancellationToken cancellationToken);

    /// <summary>Admin: approve or reject a driver.</summary>
    Task SetVerificationAsync(long driverId, VerificationRequest request, CancellationToken cancellationToken);
}
