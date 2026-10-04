using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Drivers;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Drivers. Owners see the drivers linked to them; admins see all and verify them.</summary>
[Route(ApiRoutes.Base + "/drivers")]
[Authorize(Policy = Policies.OwnerOrAdmin)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class DriversController(IDriverService drivers) : ApiControllerBase
{
    /// <summary>Drivers, by name. Filter by KYC or duty status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DriverListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DriverListItem>>> GetDrivers([FromQuery] DriverQuery query, CancellationToken cancellationToken) =>
        Ok(await drivers.GetPagedAsync(query, cancellationToken));

    /// <summary>Admin: approve or reject a driver. Approval activates the account.</summary>
    [HttpPut("{id:long}/verification")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetVerification(long id, VerificationRequest request, CancellationToken cancellationToken)
    {
        await drivers.SetVerificationAsync(id, request, cancellationToken);
        return NoContent();
    }
}
