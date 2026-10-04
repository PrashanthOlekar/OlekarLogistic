using Microsoft.AspNetCore.Mvc;

namespace ProCargo.API.Controllers;

/// <summary>
/// Shared setup for every controller: JSON in and out, and the error responses any endpoint can give.
/// Controllers stay thin: they receive the request, call one application service and choose the status code.
/// </summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
}
