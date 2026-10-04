namespace ProCargo.Application.Features.Registration;

/// <summary>A new driver account.</summary>
/// <param name="LicenceClass">LMV, TRANSPORT, HGMV or HPMV.</param>
/// <param name="OwnerMobile">The lorry owner the driver works for. Leave empty if they drive their own truck.</param>
public sealed record RegisterDriverRequest(
    string FullName,
    string Mobile,
    string Code,
    string? Email,
    string LicenceNumber,
    string LicenceClass,
    DateOnly LicenceExpiry,
    string? AadhaarLast4,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? OwnerMobile);
