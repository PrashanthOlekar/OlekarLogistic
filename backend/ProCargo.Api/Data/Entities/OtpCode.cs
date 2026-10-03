namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A one-time sign-in or sign-up code. Only a hash of the code is stored. Table: OtpCodes.
/// </summary>
public class OtpCode
{
    public long OtpCodeId { get; set; }

    public string Mobile { get; set; } = "";

    // Login | Signup | Pickup | Delivery | BankChange
    public string Purpose { get; set; } = "Login";

    public long? TripId { get; set; }

    public string CodeHash { get; set; } = "";

    public byte Attempts { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
