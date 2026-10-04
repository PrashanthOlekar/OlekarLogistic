namespace ProCargo.Application.Features.Registration;

/// <summary>A cleaned-up driver registration, ready to save.</summary>
public sealed record NewDriverAccount(
    string FullName,
    string Mobile,
    string? Email,
    long? OwnerId,
    string LicenceNumber,
    string LicenceClass,
    DateOnly LicenceExpiry,
    string? AadhaarLast4,
    string? EmergencyContactName,
    string? EmergencyContactPhone);
