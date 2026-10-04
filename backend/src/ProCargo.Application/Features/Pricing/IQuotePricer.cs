using ProCargo.Domain.Entities;
using ProCargo.Domain.Pricing;

namespace ProCargo.Application.Features.Pricing;

/// <summary>Prices trips with the current rate card and settings.</summary>
public interface IQuotePricer
{
    Task<PriceBreakdown> PriceAsync(VehicleType vehicleType, City from, City to, CancellationToken cancellationToken);

    /// <summary>A new quote (not saved yet), valid for QuoteValidityMinutes.</summary>
    Task<Quote> CreateQuoteAsync(VehicleType vehicleType, City from, City to, CancellationToken cancellationToken);
}
