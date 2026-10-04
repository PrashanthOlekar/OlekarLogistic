using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Who did what, and when.</summary>
[Route(ApiRoutes.Base + "/audit-logs")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class AuditLogsController(IAuditLogService auditLogs) : ApiControllerBase
{
    /// <summary>Recorded actions, newest first: approvals, payments, cancellations, payouts, blocks.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogItem>>> GetAuditLogs([FromQuery] AuditLogQuery query, CancellationToken cancellationToken) =>
        Ok(await auditLogs.GetPagedAsync(query, cancellationToken));
}
