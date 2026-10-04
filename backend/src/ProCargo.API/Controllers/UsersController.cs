using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Users;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Accounts, for admins.</summary>
[Route(ApiRoutes.Base + "/users")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class UsersController(IUserService users) : ApiControllerBase
{
    /// <summary>Search accounts by name, mobile or email; filter by role and status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserListItem>>> GetUsers([FromQuery] UserQuery query, CancellationToken cancellationToken) =>
        Ok(await users.GetPagedAsync(query, cancellationToken));

    /// <summary>Block ("Blocked") or unblock ("Active") an account. Blocking signs the user out everywhere.</summary>
    [HttpPatch("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(long id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await users.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }
}
