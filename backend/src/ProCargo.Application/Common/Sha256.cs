using System.Security.Cryptography;
using System.Text;

namespace ProCargo.Application.Common;

/// <summary>One-way hashes for secrets that are checked but never read back (sign-in codes, refresh tokens).</summary>
public static class Sha256
{
    public static string Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
