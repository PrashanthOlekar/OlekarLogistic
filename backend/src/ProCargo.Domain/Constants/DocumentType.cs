namespace ProCargo.Domain.Constants;

/// <summary>Kinds of document (Documents.DocType) and which ones each entity may upload.</summary>
public static class DocumentType
{
    public const string Aadhaar = "Aadhaar";
    public const string Pod = "POD";
    public const string PickupPhoto = "PickupPhoto";

    public static readonly IReadOnlyDictionary<string, string[]> AllowedByEntity = new Dictionary<string, string[]>
    {
        [DocumentEntity.Owner] = ["Aadhaar", "PAN", "CancelledCheque", "GST", "Other"],
        [DocumentEntity.Driver] = ["Licence", "Aadhaar", "DriverPhoto", "Other"],
        [DocumentEntity.Vehicle] = ["RC", "Insurance", "Fitness", "Permit", "PUC", "Other"],
        [DocumentEntity.Customer] = ["GST", "Other"],
    };

    public static bool IsAllowed(string? entityType, string? docType) =>
        entityType is not null
        && docType is not null
        && AllowedByEntity.TryGetValue(entityType, out string[]? allowed)
        && allowed.Contains(docType);
}
