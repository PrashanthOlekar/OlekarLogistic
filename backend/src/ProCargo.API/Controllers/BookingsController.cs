using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Bookings;
using ProCargo.Application.Features.Common;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>
/// Bookings: a customer's request to move goods.
/// Customers create, pay and cancel their own; admins see every booking.
/// </summary>
[Route(ApiRoutes.Base + "/bookings")]
[Authorize(Policy = Policies.CustomerOrAdmin)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class BookingsController(IBookingService bookings) : ApiControllerBase
{
    /// <summary>Bookings, newest first. Customers see their own; admins see all. Filter by status, search by number, city or customer.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookingListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookingListItem>>> GetBookings([FromQuery] BookingQuery query, CancellationToken cancellationToken) =>
        Ok(await bookings.GetPagedAsync(query, cancellationToken));

    /// <summary>One booking with its price, payment, truck, handover codes (customer only) and invoice.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(BookingDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDetail>> GetBooking(long id, CancellationToken cancellationToken) =>
        Ok(await bookings.GetAsync(id, cancellationToken));

    /// <summary>Create a booking. It is priced straight away and waits for payment.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CustomerOnly)]
    [ProducesResponseType(typeof(CreatedBooking), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatedBooking>> CreateBooking(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        CreatedBooking created = await bookings.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBooking), new { id = created.Id }, created);
    }

    /// <summary>Get a fresh quote after the old one expired.</summary>
    [HttpPost("{id:long}/quotes")]
    [Authorize(Policy = Policies.CustomerOnly)]
    [ProducesResponseType(typeof(CreatedResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedResource>> CreateQuote(long id, CancellationToken cancellationToken) =>
        Created($"/{ApiRoutes.Base}/bookings/{id}", await bookings.RequoteAsync(id, cancellationToken));

    /// <summary>Pay the open quote. Test mode marks it paid straight away until a payment gateway is connected.</summary>
    /// <response code="501">Live payments are not connected yet.</response>
    [HttpPost("{id:long}/payments")]
    [Authorize(Policy = Policies.CustomerOnly)]
    [ProducesResponseType(typeof(PaymentReceipt), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<PaymentReceipt>> PayBooking(long id, PayBookingRequest request, CancellationToken cancellationToken) =>
        Created($"/{ApiRoutes.Base}/bookings/{id}", await bookings.PayAsync(id, request, cancellationToken));

    /// <summary>Cancel before the goods are loaded. Frees the truck and refunds the payment.</summary>
    [HttpPost("{id:long}/cancellation")]
    [Authorize(Policy = Policies.CustomerOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(long id, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        await bookings.CancelAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Admin: verified, free trucks of the right type and size, each with its owner's free drivers.</summary>
    [HttpGet("{id:long}/assignable-vehicles")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(IReadOnlyList<AssignableVehicle>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AssignableVehicle>>> GetAssignableVehicles(long id, CancellationToken cancellationToken) =>
        Ok(await bookings.GetAssignableVehiclesAsync(id, cancellationToken));
}
