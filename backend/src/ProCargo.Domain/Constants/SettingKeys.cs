namespace ProCargo.Domain.Constants;

/// <summary>Keys of the Settings table, with the value used when a row is missing.</summary>
public static class SettingKeys
{
    public const string CommissionPercent = "CommissionPercent";
    public const string GstOnFreightPercent = "GstOnFreightPercent";
    public const string OtpExpiryMinutes = "OtpExpiryMinutes";
    public const string OtpMaxAttempts = "OtpMaxAttempts";
    public const string QuoteValidityMinutes = "QuoteValidityMinutes";

    public const decimal DefaultCommissionPercent = 7m;
    public const decimal DefaultGstOnFreightPercent = 5m;
    public const int DefaultOtpExpiryMinutes = 10;
    public const int DefaultOtpMaxAttempts = 5;
    public const int DefaultQuoteValidityMinutes = 30;
}
