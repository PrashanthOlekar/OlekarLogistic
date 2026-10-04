namespace ProCargo.Domain.Common;

/// <summary>Indian mobile numbers.</summary>
public static class MobileNumber
{
    public const string InvalidMessage = "Enter a valid 10-digit mobile number.";

    /// <summary>
    /// Returns a clean 10-digit Indian mobile number, or null if the input isn't one.
    /// Accepts "+91 98450 12345", "098450-12345" and similar.
    /// </summary>
    public static string? TryNormalize(string? input)
    {
        string digits = new((input ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length > 10)
        {
            digits = digits[^10..];
        }

        bool isValid = digits.Length == 10 && "6789".Contains(digits[0]);
        return isValid ? digits : null;
    }

    public static bool IsValid(string? input) => TryNormalize(input) is not null;
}
