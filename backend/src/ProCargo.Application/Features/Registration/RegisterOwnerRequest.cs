namespace ProCargo.Application.Features.Registration;

/// <summary>A new lorry owner account with KYC and the bank account for payouts.</summary>
/// <param name="Pan">PAN, for example ABCDE1234F. Stored encrypted.</param>
/// <param name="AadhaarLast4">Only the last 4 digits; the full number is never stored.</param>
/// <param name="AccountNumber">Bank account number. Stored encrypted.</param>
/// <param name="Ifsc">IFSC, for example HDFC0001234.</param>
public sealed record RegisterOwnerRequest(
    string FullName,
    string Mobile,
    string Code,
    string? Email,
    string? BusinessName,
    string Pan,
    string AadhaarLast4,
    string AccountHolder,
    string AccountNumber,
    string Ifsc,
    string? BankName);
