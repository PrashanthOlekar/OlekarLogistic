using ProCargo.Domain.Entities;

namespace ProCargo.Application.Abstractions.Persistence;

/// <summary>One-time sign-in codes (only hashes are stored).</summary>
public interface IOtpRepository
{
    Task<int> CountRecentAsync(string mobile, DateTime since, CancellationToken cancellationToken);

    Task CreateAsync(string mobile, string purpose, string codeHash, DateTime expiresAt, CancellationToken cancellationToken);

    Task<OtpCode?> GetLatestActiveAsync(string mobile, string purpose, CancellationToken cancellationToken);

    Task RecordFailedAttemptAsync(long otpCodeId, CancellationToken cancellationToken);

    /// <summary>Returns false if the code was used a moment earlier by another request.</summary>
    Task<bool> MarkUsedAsync(long otpCodeId, CancellationToken cancellationToken);
}
