using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Features.Dashboards;
using ProCargo.Application.Features.Owners;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Overview pages.</summary>
[Route(ApiRoutes.Base + "/dashboards")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class DashboardsController(IOwnerService owners, IDashboardService dashboards) : ApiControllerBase
{
    /// <summary>The signed-in owner's totals: fleet, drivers, active trips, earnings and payout account.</summary>
    [HttpGet("owner")]
    [Authorize(Policy = Policies.OwnerOnly)]
    [ProducesResponseType(typeof(OwnerDashboard), StatusCodes.Status200OK)]
    public async Task<ActionResult<OwnerDashboard>> GetOwnerDashboard(CancellationToken cancellationToken) =>
        Ok(await owners.GetMyDashboardAsync(cancellationToken));

    /// <summary>Operations counters, work queues and chart data.</summary>
    [HttpGet("admin")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(AdminDashboard), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboard>> GetAdminDashboard(CancellationToken cancellationToken) =>
        Ok(await dashboards.GetAdminAsync(cancellationToken));
}
