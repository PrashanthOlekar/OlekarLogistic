using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Vehicles;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Lorries. Owners add and manage their own; admins see all and verify them.</summary>
[Route(ApiRoutes.Base + "/vehicles")]
[Authorize(Policy = Policies.OwnerOrAdmin)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class VehiclesController(IVehicleService vehicles) : ApiControllerBase
{
    /// <summary>Vehicles with their documents. Owners see their own; admins see all.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VehicleListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<VehicleListItem>>> GetVehicles([FromQuery] VehicleQuery query, CancellationToken cancellationToken) =>
        Ok(await vehicles.GetPagedAsync(query, cancellationToken));

    /// <summary>Add a lorry. It is Pending until an admin approves it.</summary>
    /// <response code="409">A vehicle with this registration number already exists.</response>
    [HttpPost]
    [Authorize(Policy = Policies.OwnerOnly)]
    [ProducesResponseType(typeof(CreatedResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedResource>> CreateVehicle(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        CreatedResource created = await vehicles.CreateAsync(request, cancellationToken);
        return Created($"/{ApiRoutes.Base}/vehicles", created);
    }

    /// <summary>Change availability (Available, Busy, Maintenance) and/or the regular driver.</summary>
    [HttpPatch("{id:long}")]
    [Authorize(Policy = Policies.OwnerOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVehicle(long id, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        await vehicles.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Admin: approve or reject a vehicle (a reason is required to reject).</summary>
    [HttpPut("{id:long}/verification")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetVerification(long id, VerificationRequest request, CancellationToken cancellationToken)
    {
        await vehicles.SetVerificationAsync(id, request, cancellationToken);
        return NoContent();
    }
}
