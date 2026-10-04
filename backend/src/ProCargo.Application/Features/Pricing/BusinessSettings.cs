using System.Globalization;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Pricing;

namespace ProCargo.Application.Features.Pricing;

/// <summary>
/// Typed access to the Settings table. Change a value in the table and the next request uses it.
/// </summary>
public sealed class BusinessSettings(IReadOnlyDictionary<string, string> values)
{
    public decimal CommissionPercent => GetDecimal(SettingKeys.CommissionPercent, SettingKeys.DefaultCommissionPercent);

    public decimal GstOnFreightPercent => GetDecimal(SettingKeys.GstOnFreightPercent, SettingKeys.DefaultGstOnFreightPercent);

    public int OtpExpiryMinutes => GetInt(SettingKeys.OtpExpiryMinutes, SettingKeys.DefaultOtpExpiryMinutes);

    public int OtpMaxAttempts => GetInt(SettingKeys.OtpMaxAttempts, SettingKeys.DefaultOtpMaxAttempts);

    public int QuoteValidityMinutes => GetInt(SettingKeys.QuoteValidityMinutes, SettingKeys.DefaultQuoteValidityMinutes);

    public PricingRates PricingRates => new(CommissionPercent, GstOnFreightPercent);

    private decimal GetDecimal(string key, decimal fallback) =>
        values.TryGetValue(key, out string? text)
        && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : fallback;

    private int GetInt(string key, int fallback) => (int)GetDecimal(key, fallback);
}
