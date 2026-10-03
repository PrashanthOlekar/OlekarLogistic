namespace ProCargo.Api.Data.Entities;

/// <summary>
/// Bank account where an owner's payouts are sent. Table: OwnerBankAccounts.
/// </summary>
public class OwnerBankAccount
{
    public long OwnerBankAccountId { get; set; }

    public long OwnerId { get; set; }

    public string AccountHolder { get; set; } = "";

    public byte[] AccountNumberEncrypted { get; set; } = Array.Empty<byte>();

    public string AccountLast4 { get; set; } = "";

    public string IFSC { get; set; } = "";

    public string? BankName { get; set; }

    public string PennyDropStatus { get; set; } = "Pending";

    public bool IsPrimary { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
