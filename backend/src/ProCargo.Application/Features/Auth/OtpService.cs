using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Features.Pricing;

namespace ProCargo.Application.Features.Auth;

internal sealed class OtpService(
    IOtpRepository codes,
    IBusinessSettingsProvider settingsProvider,
    IOptions<OtpOptions> options,
    TimeProvider clock) : IOtpService
{
    /// <summary>A random number with the given number of digits, e.g. 6 → "482915".</summary>
    public static string NewCode(int digits)
    {
        int min = (int)Math.Pow(10, digits - 1);
        int max = (int)Math.Pow(10, digits);
        return RandomNumberGenerator.GetInt32(min, max).ToString();
    }

    public async Task<string> IssueAsync(string mobile, string purpose, CancellationToken cancellationToken)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;

        int recentCodes = await codes.CountRecentAsync(mobile, now.AddMinutes(-10), cancellationToken);
        if (recentCodes >= options.Value.MaxCodesPerTenMinutes)
        {
            throw new TooManyRequestsException("Too many codes requested. Try again in 10 minutes.");
        }

        BusinessSettings settings = await settingsProvider.GetAsync(cancellationToken);
        string code = NewCode(6);

        await codes.CreateAsync(
            mobile,
            purpose,
            OtpCodeHasher.Hash(mobile, code),
            now.AddMinutes(settings.OtpExpiryMinutes),
            cancellationToken);

        return code;
    }

    public async Task VerifyAsync(string mobile, string code, string purpose, CancellationToken cancellationToken)
    {
        var otp = await codes.GetLatestActiveAsync(mobile, purpose, cancellationToken)
            ?? throw new BusinessRuleException("This code has expired. Request a new one.");

        BusinessSettings settings = await settingsProvider.GetAsync(cancellationToken);
        if (otp.Attempts >= settings.OtpMaxAttempts)
        {
            throw new BusinessRuleException("Too many wrong tries. Request a new code.");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(otp.CodeHash),
                System.Text.Encoding.ASCII.GetBytes(OtpCodeHasher.Hash(mobile, (code ?? string.Empty).Trim()))))
        {
            await codes.RecordFailedAttemptAsync(otp.OtpCodeId, cancellationToken);
            throw new BusinessRuleException("That code is not correct.");
        }

        bool marked = await codes.MarkUsedAsync(otp.OtpCodeId, cancellationToken);
        if (!marked)
        {
            throw new BusinessRuleException("This code has already been used. Request a new one.");
        }
    }
}
