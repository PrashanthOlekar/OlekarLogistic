namespace ProCargo.Application.Features.Documents;

/// <summary>The form fields sent with a KYC or vehicle document.</summary>
/// <param name="EntityType">Owner, Driver, Customer or Vehicle.</param>
/// <param name="EntityId">The vehicle's id when EntityType is Vehicle; otherwise leave empty (your own profile is used).</param>
/// <param name="DocType">For example RC, Insurance, PAN, Licence. Each EntityType allows its own list.</param>
/// <param name="DocumentNumber">Policy or permit number. Never send an Aadhaar number; it is not stored.</param>
public sealed record UploadDocumentRequest(
    string EntityType,
    long? EntityId,
    string DocType,
    string? DocumentNumber,
    DateOnly? ExpiryDate);
