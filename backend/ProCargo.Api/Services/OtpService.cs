using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;

namespace ProCargo.Api.Services;

/// <summary>
/// One-time codes for sign-in and sign-up.
/// Only a hash of each code is stored, so a database leak does not reveal codes.
/// </summary>
public class OtpService
{
    private const int MaxCodesPerTenMinutes = 5;

    private readonly ProCargoDbContext _db;
    private readonly SettingsService _settings;

    public OtpService(ProCargoDbContext db, SettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    /// <summary>A random number with the given number of digits, e.g. 6 → "482915".</summary>
    public static string NewCode(int digits)
    {
        int min = (int)Math.Pow(10, digits - 1);
        int max = (int)Math.Pow(10, digits);
        return RandomNumberGenerator.GetInt32(min, max).ToString();
    }

    /// <summary>Creates a code and returns it, so the caller can send it by SMS.</summary>
    public async Task<string> IssueAsync(string mobile, string purpose)
    {
        int recentCodes = await _db.OtpCodes.CountAsync(otp =>
            otp.Mobile == mobile && otp.CreatedAt > DateTime.UtcNow.AddMinutes(-10));

        if (recentCodes >= MaxCodesPerTenMinutes)
        {
            throw new ApiException(StatusCodes.Status429TooManyRequests, "Too many codes requested. Try again in 10 minutes.");
        }

        string code = NewCode(6);
        int validMinutes = await _settings.GetIntAsync("OtpExpiryMinutes", 10);

        _db.OtpCodes.Add(new OtpCode
        {
            Mobile = mobile,
            Purpose = purpose,
            CodeHash = Hash(mobile, code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(validMinutes),
        });
        await _db.SaveChangesAsync();

        return code;
    }

    /// <summary>Checks a code. Throws a friendly error if it is wrong, expired or used too often.</summary>
    public async Task VerifyAsync(string mobile, string code, string purpose)
    {
        OtpCode otp = await _db.OtpCodes
            .Where(row => row.Mobile == mobile
                && row.Purpose == purpose
                && row.UsedAt == null
                && row.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw ApiException.BadRequest("This code has expired. Request a new one.");

        int maxAttempts = await _settings.GetIntAsync("OtpMaxAttempts", 5);
        if (otp.Attempts >= maxAttempts)
        {
            throw ApiException.BadRequest("Too many wrong tries. Request a new code.");
        }

        if (otp.CodeHash != Hash(mobile, code.Trim()))
        {
            otp.Attempts++;
            await _db.SaveChangesAsync();
            throw ApiException.BadRequest("That code is not correct.");
        }

        otp.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static string Hash(string mobile, string code)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{mobile}:{code}:procargo"));
        return Convert.ToHexString(bytes);
    }
}
