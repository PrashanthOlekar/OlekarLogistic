namespace ProCargo.Application.Features.Pricing;

public interface IPriceEstimateService
{
    Task<PriceEstimateResponse> EstimateAsync(PriceEstimateRequest request, CancellationToken cancellationToken);
}
