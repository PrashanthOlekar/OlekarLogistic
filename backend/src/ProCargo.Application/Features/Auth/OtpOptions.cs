namespace ProCargo.Application.Features.Auth;

/// <summary>Settings for sign-in codes, from the "Otp" section of appsettings.</summary>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>Most codes one mobile number may request in 10 minutes.</summary>
    public int MaxCodesPerTenMinutes { get; set; } = 5;

    /// <summary>
    /// Return the code in the response so you can test without SMS.
    /// The API forces this off outside Development.
    /// </summary>
    public bool ShowCodeInResponse { get; set; }
}
