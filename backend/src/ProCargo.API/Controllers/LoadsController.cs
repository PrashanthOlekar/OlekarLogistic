using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Features.Loads;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Paid loads a lorry owner's free trucks can carry. To take one, POST /trips.</summary>
[Route(ApiRoutes.Base + "/loads")]
[Authorize(Policy = Policies.OwnerOnly)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class LoadsController(ILoadService loads) : ApiControllerBase
{
    /// <summary>Loads matched to my verified, available trucks. Empty until my KYC is approved.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(LoadsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoadsResponse>> GetLoads(CancellationToken cancellationToken) =>
        Ok(await loads.GetAvailableAsync(cancellationToken));

    /// <summary>Hide a load from my list (declines it).</summary>
    [HttpDelete("{bookingId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeclineLoad(long bookingId, CancellationToken cancellationToken)
    {
        await loads.DeclineAsync(bookingId, cancellationToken);
        return NoContent();
    }
}
