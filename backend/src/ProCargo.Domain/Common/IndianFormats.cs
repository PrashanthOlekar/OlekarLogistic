using System.Text.RegularExpressions;

namespace ProCargo.Domain.Common;

/// <summary>Formats of Indian identity and vehicle numbers, and how to clean them up.</summary>
public static partial class IndianFormats
{
    /// <summary>PAN, for example ABCDE1234F.</summary>
    [GeneratedRegex("^[A-Z]{5}[0-9]{4}[A-Z]$")]
    public static partial Regex Pan();

    /// <summary>IFSC, for example HDFC0001234.</summary>
    [GeneratedRegex("^[A-Z]{4}0[A-Z0-9]{6}$")]
    public static partial Regex Ifsc();

    /// <summary>Exactly four digits (the last 4 of Aadhaar).</summary>
    [GeneratedRegex("^[0-9]{4}$")]
    public static partial Regex FourDigits();

    /// <summary>Vehicle registration without spaces, for example KA01AB4521 or MH12K1234.</summary>
    [GeneratedRegex("^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{4}$")]
    public static partial Regex VehicleRegistration();

    /// <summary>"KA 01 AB 4521" → "KA01AB4521".</summary>
    public static string CleanRegistration(string? input) =>
        new string((input ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    /// <summary>"ka-01 2011 0012345" → "KA0120110012345".</summary>
    public static string CleanLicence(string? input) =>
        (input ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();

    /// <summary>Keeps only the digits of a bank account number.</summary>
    public static string CleanAccountNumber(string? input) =>
        new((input ?? string.Empty).Where(char.IsDigit).ToArray());

    public static string CleanUpper(string? input) => (input ?? string.Empty).Trim().ToUpperInvariant();
}
