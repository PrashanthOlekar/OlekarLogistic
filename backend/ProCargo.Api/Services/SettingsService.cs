using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Data;

namespace ProCargo.Api.Services;

/// <summary>
/// Reads business settings from the Settings table (commission %, GST %, OTP rules...).
/// Change a value in the table and the API uses it on the next request.
/// </summary>
public class SettingsService
{
    private readonly ProCargoDbContext _db;

    public SettingsService(ProCargoDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal fallback)
    {
        string? value = await _db.Settings
            .Where(setting => setting.SettingKey == key)
            .Select(setting => setting.SettingValue)
            .FirstOrDefaultAsync();

        bool parsed = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number);
        return parsed ? number : fallback;
    }

    public async Task<int> GetIntAsync(string key, int fallback)
    {
        decimal value = await GetDecimalAsync(key, fallback);
        return (int)value;
    }
}
