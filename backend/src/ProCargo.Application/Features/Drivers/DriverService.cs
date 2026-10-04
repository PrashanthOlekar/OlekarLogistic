using FluentValidation;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Constants;

namespace ProCargo.Application.Features.Drivers;

internal sealed class DriverService(
    IDriverRepository drivers,
    ICurrentProfiles profiles,
    ICurrentUser currentUser,
    IAuditTrail auditTrail,
    IValidator<VerificationRequest> validator,
    ILogger<DriverService> logger) : IDriverService
{
    public async Task<PagedResult<DriverListItem>> GetPagedAsync(DriverQuery query, CancellationToken cancellationToken)
    {
        long? ownerId = currentUser.IsInRole(Roles.Admin)
            ? null
            : (await profiles.GetOwnerAsync(cancellationToken)).OwnerId;

        return await drivers.GetPagedAsync(query, ownerId, cancellationToken);
    }

    public async Task SetVerificationAsync(long driverId, VerificationRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        bool found = await drivers.SetVerificationAsync(driverId, request.Approve, request.Reason?.Trim(), currentUser.UserId, cancellationToken);
        if (!found)
        {
            throw new NotFoundException("Driver not found.");
        }

        string decision = request.Approve ? "Approved" : "Rejected";
        await auditTrail.RecordAsync($"Driver.{decision}", "Driver", driverId, new { request.Reason }, cancellationToken);
        logger.LogInformation("Driver {DriverId} KYC {Decision} by {AdminId}", driverId, decision, currentUser.UserId);
    }
}
