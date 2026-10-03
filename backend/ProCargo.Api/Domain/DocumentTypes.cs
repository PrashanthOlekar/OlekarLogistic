namespace ProCargo.Api.Domain;

/// <summary>What a document belongs to (Documents.EntityType).</summary>
public static class DocumentEntity
{
    public const string Owner = "Owner";
    public const string Driver = "Driver";
    public const string Vehicle = "Vehicle";
    public const string Customer = "Customer";
    public const string Trip = "Trip";
}

/// <summary>Kinds of document (Documents.DocType) and which ones each entity may upload.</summary>
public static class DocumentType
{
    public const string Aadhaar = "Aadhaar";
    public const string Pod = "POD";
    public const string PickupPhoto = "PickupPhoto";

    public static readonly IReadOnlyDictionary<string, string[]> AllowedByEntity = new Dictionary<string, string[]>
    {
        [DocumentEntity.Owner] = new[] { "Aadhaar", "PAN", "CancelledCheque", "GST", "Other" },
        [DocumentEntity.Driver] = new[] { "Licence", "Aadhaar", "DriverPhoto", "Other" },
        [DocumentEntity.Vehicle] = new[] { "RC", "Insurance", "Fitness", "Permit", "PUC", "Other" },
        [DocumentEntity.Customer] = new[] { "GST", "Other" },
    };
}
