namespace ProCargo.Api.Contracts;

// What the portal sends to the /api/auth endpoints.

/// <summary>Purpose is "Login" or "Signup".</summary>
public record SendOtpRequest(string Mobile, string Purpose);

public record LoginRequest(string Mobile, string Code);

public record RegisterCustomerRequest(
    string FullName,
    string Mobile,
    string Code,
    string? Email,
    string? CompanyName,
    string? Gstin);

public record RegisterOwnerRequest(
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

public record RegisterDriverRequest(
    string FullName,
    string Mobile,
    string Code,
    string LicenceNumber,
    string LicenceClass,
    DateOnly LicenceExpiry,
    string? AadhaarLast4,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? OwnerMobile);
