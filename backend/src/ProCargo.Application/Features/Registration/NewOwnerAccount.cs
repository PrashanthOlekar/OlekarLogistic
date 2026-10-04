namespace ProCargo.Application.Features.Registration;

/// <summary>A cleaned-up owner registration, ready to save. PAN and account number are already encrypted.</summary>
public sealed record NewOwnerAccount(
    string FullName,
    string Mobile,
    string? Email,
    string? BusinessName,
    byte[] PanEncrypted,
    string PanLast4,
    string AadhaarLast4,
    string AccountHolder,
    byte[] AccountNumberEncrypted,
    string AccountLast4,
    string Ifsc,
    string? BankName);
