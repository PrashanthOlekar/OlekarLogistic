using Microsoft.AspNetCore.Mvc;

namespace ProCargo.API.Controllers;

/// <summary>
/// Shared setup for every controller: the error responses any endpoint can give.
/// (No [Produces] here: it would overwrite the application/problem+json content type of validation errors.)
/// Controllers stay thin: they receive the request, call one application service and choose the status code.
/// </summary>
[ApiController]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
}
