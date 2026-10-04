using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Payments;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>Customer payments. To pay a booking, POST /bookings/{id}/payments.</summary>
[Route(ApiRoutes.Base + "/payments")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class PaymentsController(IPaymentService payments) : ApiControllerBase
{
    /// <summary>Payments, newest first. Search by booking number, customer or gateway reference.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PaymentListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentListItem>>> GetPayments([FromQuery] PaymentQuery query, CancellationToken cancellationToken) =>
        Ok(await payments.GetPagedAsync(query, cancellationToken));
}
