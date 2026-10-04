using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Features.Approvals;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>The admin's KYC work queue.</summary>
[Route(ApiRoutes.Base + "/approvals")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class ApprovalsController(IApprovalService approvals) : ApiControllerBase
{
    /// <summary>Owners, drivers and vehicles waiting for a decision, with their documents.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PendingApprovals), StatusCodes.Status200OK)]
    public async Task<ActionResult<PendingApprovals>> GetPendingApprovals(CancellationToken cancellationToken) =>
        Ok(await approvals.GetPendingAsync(cancellationToken));
}
