using ProCargo.Application.Abstractions.Persistence;

namespace ProCargo.Application.Features.Pricing;

/// <summary>Scoped: the settings are read from the database at most once per request.</summary>
internal sealed class BusinessSettingsProvider(ISettingsRepository repository) : IBusinessSettingsProvider
{
    private BusinessSettings? _settings;

    public async Task<BusinessSettings> GetAsync(CancellationToken cancellationToken)
    {
        _settings ??= new BusinessSettings(await repository.GetAllAsync(cancellationToken));
        return _settings;
    }
}
