namespace ProCargo.Application.Features.Loads;

/// <summary>Paid loads for lorry owners. Taking a load is POST /trips.</summary>
public interface ILoadService
{
    Task<LoadsResponse> GetAvailableAsync(CancellationToken cancellationToken);

    Task DeclineAsync(long bookingId, CancellationToken cancellationToken);
}
