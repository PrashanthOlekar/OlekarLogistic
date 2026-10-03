namespace ProCargo.Api.Common;

public static class MobileNumber
{
    /// <summary>
    /// Returns a clean 10-digit Indian mobile number.
    /// Accepts "+91 98450 12345", "098450-12345" and similar.
    /// </summary>
    public static string Normalize(string? input)
    {
        string digits = new((input ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length > 10)
        {
            digits = digits[^10..];
        }

        bool isValid = digits.Length == 10 && "6789".Contains(digits[0]);
        Guard.Require(isValid, "Enter a valid 10-digit mobile number.");

        return digits;
    }
}
