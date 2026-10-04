using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Features.Owners;
using ProCargo.Domain.Entities;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class OwnerRepository(StoredProcedureExecutor database) : IOwnerRepository
{
    public Task<Owner?> GetByIdAsync(long ownerId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Owner>(StoredProcedures.OwnerGetById, new { OwnerId = ownerId }, cancellationToken);

    public Task<Owner?> GetByUserIdAsync(long userId, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Owner>(StoredProcedures.OwnerGetByUserId, new { UserId = userId }, cancellationToken);

    public Task<Owner?> GetByMobileAsync(string mobile, CancellationToken cancellationToken) =>
        database.QuerySingleOrDefaultAsync<Owner>(StoredProcedures.OwnerGetByMobile, new { Mobile = mobile }, cancellationToken);

    public async Task<bool> SetVerificationAsync(long ownerId, bool approve, string? reason, long adminUserId, CancellationToken cancellationToken) =>
        await database.ExecuteScalarAsync<int>(
            StoredProcedures.OwnerSetVerification,
            new { OwnerId = ownerId, Approve = approve, Reason = reason, AdminUserId = adminUserId },
            cancellationToken) > 0;

    public Task<OwnerDashboard?> GetDashboardAsync(long ownerId, DateTime monthStartUtc, CancellationToken cancellationToken) =>
        database.QueryMultipleAsync(
            StoredProcedures.DashboardGetOwner,
            new { OwnerId = ownerId, MonthStartAt = monthStartUtc },
            async grid =>
            {
                OwnerDashboard? dashboard = await grid.ReadFirstOrDefaultAsync<OwnerDashboard>();
                OwnerBankSummary? bank = await grid.ReadFirstOrDefaultAsync<OwnerBankSummary>();
                if (dashboard is not null)
                {
                    dashboard.Bank = bank;
                }
                return dashboard;
            },
            cancellationToken);
}
