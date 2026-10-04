using ProCargo.Application.Common;

namespace ProCargo.Application.Features.Auth;

/// <summary>Only a hash of each code is stored, so a database leak does not reveal codes.</summary>
internal static class OtpCodeHasher
{
    public static string Hash(string mobile, string code) => Sha256.Hex($"{mobile}:{code}:procargo");
}
