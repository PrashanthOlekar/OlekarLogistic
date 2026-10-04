using FluentValidation;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Settlements;

internal sealed class SettlementService(
    ISettlementRepository settlements,
    ICurrentProfiles profiles,
    ICurrentUser currentUser,
    IAuditTrail auditTrail,
    IValidator<RecordPayoutRequest> validator,
    ILogger<SettlementService> logger) : ISettlementService
{
    public async Task<PagedResult<SettlementListItem>> GetPagedAsync(SettlementQuery query, CancellationToken cancellationToken)
    {
        long? ownerId = currentUser.IsInRole(Roles.Admin)
            ? null
            : (await profiles.GetOwnerAsync(cancellationToken)).OwnerId;

        return await settlements.GetPagedAsync(query, ownerId, cancellationToken);
    }

    public async Task RecordPayoutAsync(long settlementId, RecordPayoutRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        Settlement settlement = await settlements.GetByIdAsync(settlementId, cancellationToken)
            ?? throw new NotFoundException("Settlement not found.");

        if (settlement.Status != SettlementStatus.Approved)
        {
            throw new BusinessRuleException("Approve the trip's POD before releasing the payout.");
        }

        string utr = request.Utr.Trim();
        await settlements.RecordPayoutAsync(settlementId, utr, currentUser.UserId, cancellationToken);

        await auditTrail.RecordAsync("Settlement.Released", "Settlement", settlementId, new { settlement.NetAmount, Utr = utr }, cancellationToken);
        logger.LogInformation("Payout {SettlementId} of ₹{Amount} released by {AdminId}", settlementId, settlement.NetAmount, currentUser.UserId);
    }
}
