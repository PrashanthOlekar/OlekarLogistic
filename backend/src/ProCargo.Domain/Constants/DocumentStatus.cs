namespace ProCargo.Domain.Constants;

/// <summary>Documents.Status</summary>
public static class DocumentStatus
{
    public const string Pending = "Pending";
    public const string Verified = "Verified";
    public const string Rejected = "Rejected";
    public const string Expired = "Expired";

    public static readonly IReadOnlyList<string> All = [Pending, Verified, Rejected, Expired];
}
