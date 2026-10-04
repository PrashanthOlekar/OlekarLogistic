using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Settlements;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Owner payouts: created at delivery, approved with the POD, released by the admin with the bank's UTR.</summary>
[Route(ApiRoutes.Base + "/settlements")]
[Authorize(Policy = Policies.OwnerOrAdmin)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class SettlementsController(ISettlementService settlements) : ApiControllerBase
{
    /// <summary>Payouts, newest first. Owners see their own; admins see all.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SettlementListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SettlementListItem>>> GetSettlements([FromQuery] SettlementQuery query, CancellationToken cancellationToken) =>
        Ok(await settlements.GetPagedAsync(query, cancellationToken));

    /// <summary>Admin: record a payout already sent from the bank, with its UTR.</summary>
    [HttpPut("{id:long}/payout")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordPayout(long id, RecordPayoutRequest request, CancellationToken cancellationToken)
    {
        await settlements.RecordPayoutAsync(id, request, cancellationToken);
        return NoContent();
    }
}
