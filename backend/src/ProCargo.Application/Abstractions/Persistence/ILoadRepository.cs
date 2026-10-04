using ProCargo.Application.Features.Loads;

namespace ProCargo.Application.Abstractions.Persistence;

public interface ILoadRepository
{
    /// <summary>Paid loads the owner's free trucks could carry (declined ones hidden), and those free trucks.</summary>
    Task<AvailableLoadsData> GetAvailableAsync(long ownerId, CancellationToken cancellationToken);

    /// <summary>Hides a load from this owner's list.</summary>
    Task DeclineAsync(long bookingId, long ownerId, CancellationToken cancellationToken);
}
