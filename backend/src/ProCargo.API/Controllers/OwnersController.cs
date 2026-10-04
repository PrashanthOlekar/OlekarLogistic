using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Owners;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Lorry owners' KYC decisions.</summary>
[Route(ApiRoutes.Base + "/owners")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class OwnersController(IOwnerService owners) : ApiControllerBase
{
    /// <summary>Approve or reject an owner's KYC. Approval activates the account.</summary>
    [HttpPut("{id:long}/verification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetVerification(long id, VerificationRequest request, CancellationToken cancellationToken)
    {
        await owners.SetVerificationAsync(id, request, cancellationToken);
        return NoContent();
    }
}
