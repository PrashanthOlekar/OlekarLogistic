using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace ProCargo.Api.Services;

/// <summary>
/// Encrypts private data: PAN, bank account numbers and trip handover codes.
/// Uses ASP.NET Core Data Protection. In production, keep the keys in
/// Azure Blob Storage + Key Vault so they survive restarts (see Program setup).
/// </summary>
public class PersonalDataProtector
{
    private readonly IDataProtector _protector;

    public PersonalDataProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ProCargo.PersonalData.v1");
    }

    /// <summary>For VARBINARY columns (PanEncrypted, AccountNumberEncrypted).</summary>
    public byte[] ProtectToBytes(string plainText) => _protector.Protect(Encoding.UTF8.GetBytes(plainText));

    /// <summary>For text columns (PickupOtpProtected, DeliveryOtpProtected).</summary>
    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
