namespace ProCargo.Application.Features.Pricing;

/// <summary>Loads the Settings table once per request.</summary>
public interface IBusinessSettingsProvider
{
    Task<BusinessSettings> GetAsync(CancellationToken cancellationToken);
}
