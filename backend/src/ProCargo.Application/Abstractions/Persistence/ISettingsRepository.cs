namespace ProCargo.Application.Abstractions.Persistence;

/// <summary>Business settings from the Settings table (commission %, GST %, OTP rules...).</summary>
public interface ISettingsRepository
{
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken);
}
