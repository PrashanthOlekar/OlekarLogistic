using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Features.Pricing;

namespace ProCargo.API.Controllers;

/// <summary>Instant prices before booking. No sign-in needed.</summary>
[Route(ApiRoutes.Base + "/price-estimates")]
[AllowAnonymous]
public sealed class PriceEstimatesController(IPriceEstimateService estimates) : ApiControllerBase
{
    /// <summary>Price a trip between two cities. Suggests a bigger vehicle when the load is too heavy.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PriceEstimateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriceEstimateResponse>> EstimatePrice(PriceEstimateRequest request, CancellationToken cancellationToken) =>
        Ok(await estimates.EstimateAsync(request, cancellationToken));
}
