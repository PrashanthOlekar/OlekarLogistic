using FluentValidation;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Common;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Owners;

internal sealed class OwnerService(
    IOwnerRepository owners,
    ICurrentProfiles profiles,
    ICurrentUser currentUser,
    IAuditTrail auditTrail,
    IValidator<VerificationRequest> validator,
    TimeProvider clock,
    ILogger<OwnerService> logger) : IOwnerService
{
    public async Task<OwnerDashboard> GetMyDashboardAsync(CancellationToken cancellationToken)
    {
        Owner owner = await profiles.GetOwnerAsync(cancellationToken);

        DateOnly today = IndianTime.TodayFor(clock.GetUtcNow().UtcDateTime);
        DateTime monthStartUtc = IndianTime.StartOfDayUtc(new DateOnly(today.Year, today.Month, 1));

        return await owners.GetDashboardAsync(owner.OwnerId, monthStartUtc, cancellationToken)
            ?? throw new NotFoundException("Owner not found.");
    }

    public async Task SetVerificationAsync(long ownerId, VerificationRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        bool found = await owners.SetVerificationAsync(ownerId, request.Approve, request.Reason?.Trim(), currentUser.UserId, cancellationToken);
        if (!found)
        {
            throw new NotFoundException("Owner not found.");
        }

        string decision = request.Approve ? "Approved" : "Rejected";
        await auditTrail.RecordAsync($"Owner.{decision}", "Owner", ownerId, new { request.Reason }, cancellationToken);
        logger.LogInformation("Owner {OwnerId} KYC {Decision} by {AdminId}", ownerId, decision, currentUser.UserId);
    }
}
