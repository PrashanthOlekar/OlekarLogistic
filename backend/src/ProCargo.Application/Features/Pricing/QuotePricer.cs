using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;
using ProCargo.Domain.Pricing;

namespace ProCargo.Application.Features.Pricing;

internal sealed class QuotePricer(IBusinessSettingsProvider settingsProvider, TimeProvider clock) : IQuotePricer
{
    public async Task<PriceBreakdown> PriceAsync(VehicleType vehicleType, City from, City to, CancellationToken cancellationToken)
    {
        BusinessSettings settings = await settingsProvider.GetAsync(cancellationToken);
        decimal distanceKm = DistanceEstimator.EstimateRoadKm(from, to);

        return PriceCalculator.Calculate(vehicleType, distanceKm, settings.PricingRates);
    }

    public async Task<Quote> CreateQuoteAsync(VehicleType vehicleType, City from, City to, CancellationToken cancellationToken)
    {
        BusinessSettings settings = await settingsProvider.GetAsync(cancellationToken);
        PriceBreakdown price = await PriceAsync(vehicleType, from, to, cancellationToken);

        return new Quote
        {
            DistanceKm = price.DistanceKm,
            VehicleCost = price.VehicleCost,
            DriverCost = price.DriverCost,
            LoadingCharges = 0,
            PlatformFee = price.Commission,
            TaxAmount = price.TaxAmount,
            TotalAmount = price.TotalAmount,
            OwnerPayout = price.OwnerPayout,
            ValidUntil = clock.GetUtcNow().UtcDateTime.AddMinutes(settings.QuoteValidityMinutes),
            Status = QuoteStatus.Sent,
        };
    }
}
