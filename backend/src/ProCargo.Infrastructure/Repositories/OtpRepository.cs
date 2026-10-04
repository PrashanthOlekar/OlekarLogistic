using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class OtpRepository(StoredProcedureExecutor database) : IOtpRepository
{
    public Task<int> CountRecentAsync(string mobile, DateTime since, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<int>(StoredProcedures.OtpCodeCountRecent, new { Mobile = mobile, Since = since }, cancellationToken);

    public Task CreateAsync(string mobile, string purpose, string codeHash, DateTime expiresAt, CancellationToken cancellationToken) =>
        database.ExecuteAsync(
            StoredProcedures.OtpCodeCreate,
            new { Mobile = mobile, Purpose = purpose, CodeHash = codeHash, ExpiresAt = expiresAt },
            cancellationToken);

    public Task<OtpCode?> GetLatestActiveAsync(string mobile, string purpose, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<OtpCode>(StoredProcedures.OtpCodeGetLatestActive, new { Mobile = mobile, Purpose = purpose }, cancellationToken);

    public Task RecordFailedAttemptAsync(long otpCodeId, CancellationToken cancellationToken) =>
        database.ExecuteAsync(StoredProcedures.OtpCodeRecordFailedAttempt, new { OtpCodeId = otpCodeId }, cancellationToken);

    public async Task<bool> MarkUsedAsync(long otpCodeId, CancellationToken cancellationToken) =>
        await database.ExecuteScalarAsync<int>(StoredProcedures.OtpCodeMarkUsed, new { OtpCodeId = otpCodeId }, cancellationToken) > 0;
}
