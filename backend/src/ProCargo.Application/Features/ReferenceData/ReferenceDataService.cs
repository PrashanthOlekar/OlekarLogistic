using ProCargo.Application.Abstractions.Persistence;

namespace ProCargo.Application.Features.ReferenceData;

internal sealed class ReferenceDataService(IReferenceDataRepository repository) : IReferenceDataService
{
    public Task<ReferenceDataResponse> GetAsync(CancellationToken cancellationToken) =>
        repository.GetFormListsAsync(cancellationToken);
}
