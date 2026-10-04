using System.Text;
using Microsoft.AspNetCore.DataProtection;
using ProCargo.Application.Abstractions.Security;

namespace ProCargo.Infrastructure.Security;

/// <summary>
/// Encrypts PAN, bank account numbers and trip handover codes with ASP.NET Core Data Protection.
/// The purpose string is the same as in the old API, so data encrypted before the refactor still opens.
/// In production keep the keys in Azure Blob Storage protected by Key Vault so they survive restarts.
/// </summary>
internal sealed class PersonalDataProtector(IDataProtectionProvider provider) : IPersonalDataProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("ProCargo.PersonalData.v1");

    public byte[] ProtectToBytes(string plainText) => _protector.Protect(Encoding.UTF8.GetBytes(plainText));

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
