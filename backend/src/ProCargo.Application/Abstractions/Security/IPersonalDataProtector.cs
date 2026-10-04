namespace ProCargo.Application.Abstractions.Security;

/// <summary>Encrypts private data: PAN, bank account numbers and trip handover codes.</summary>
public interface IPersonalDataProtector
{
    /// <summary>For VARBINARY columns (PanEncrypted, AccountNumberEncrypted).</summary>
    byte[] ProtectToBytes(string plainText);

    /// <summary>For text columns (PickupOtpProtected, DeliveryOtpProtected).</summary>
    string Protect(string plainText);

    string Unprotect(string protectedText);
}
