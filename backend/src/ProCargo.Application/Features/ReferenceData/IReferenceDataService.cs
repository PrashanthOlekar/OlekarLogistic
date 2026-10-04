namespace ProCargo.Application.Features.ReferenceData;

public interface IReferenceDataService
{
    Task<ReferenceDataResponse> GetAsync(CancellationToken cancellationToken);
}
