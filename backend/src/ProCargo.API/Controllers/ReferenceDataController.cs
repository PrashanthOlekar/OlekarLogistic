using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Features.ReferenceData;

namespace ProCargo.API.Controllers;

/// <summary>Lists for the booking and registration forms. No sign-in needed.</summary>
[Route(ApiRoutes.Base + "/reference-data")]
[AllowAnonymous]
public sealed class ReferenceDataController(IReferenceDataService referenceData) : ApiControllerBase
{
    /// <summary>Serviceable cities, vehicle types (with sizes) and goods categories.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ReferenceDataResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReferenceDataResponse>> GetReferenceData(CancellationToken cancellationToken) =>
        Ok(await referenceData.GetAsync(cancellationToken));
}
