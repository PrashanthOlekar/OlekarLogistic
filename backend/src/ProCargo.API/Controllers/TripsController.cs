using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Abstractions.Storage;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Trips;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>
/// Trips: one truck and driver carrying one booking.
/// Owners take loads and admins assign trucks (POST); drivers move the trip step by step;
/// admins approve the delivery proof.
/// </summary>
[Route(ApiRoutes.Base + "/trips")]
[Authorize(Policy = Policies.OwnerDriverOrAdmin)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class TripsController(ITripService trips) : ApiControllerBase
{
    private const long MaxUploadBytes = FileUploadRules.MaxFileBytes + (1024 * 1024);

    /// <summary>Trips, newest first (drivers: current trips first). Owners see theirs, drivers theirs, admins all.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TripListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TripListItem>>> GetTrips([FromQuery] TripQuery query, CancellationToken cancellationToken) =>
        Ok(await trips.GetPagedAsync(query, cancellationToken));

    /// <summary>One trip with the booking, both stops and the timeline.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TripDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripDetail>> GetTrip(long id, CancellationToken cancellationToken) =>
        Ok(await trips.GetAsync(id, cancellationToken));

    /// <summary>Put a truck and driver on a paid booking. Owners may only use their own trucks.</summary>
    /// <response code="409">The load was taken by someone else, or the truck or driver became busy.</response>
    [HttpPost]
    [Authorize(Policy = Policies.OwnerOrAdmin)]
    [ProducesResponseType(typeof(CreatedTrip), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedTrip>> AssignTrip(AssignTripRequest request, CancellationToken cancellationToken)
    {
        CreatedTrip created = await trips.AssignAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTrip), new { id = created.Id }, created);
    }

    /// <summary>Driver: on the way, reached pickup, start trip or reached destination.</summary>
    [HttpPost("{id:long}/events")]
    [Authorize(Policy = Policies.DriverOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordEvent(long id, TripEventRequest request, CancellationToken cancellationToken)
    {
        await trips.RecordEventAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Driver: enter the pickup code (goods loaded) or the delivery code (goods delivered).</summary>
    [HttpPost("{id:long}/handovers")]
    [Authorize(Policy = Policies.DriverOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmHandover(long id, HandoverRequest request, CancellationToken cancellationToken)
    {
        await trips.ConfirmHandoverAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Driver: upload the goods photo at pickup, or the signed POD at the destination (multipart form: kind, file, latitude, longitude).</summary>
    [HttpPost("{id:long}/photos")]
    [Authorize(Policy = Policies.DriverOnly)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(typeof(CreatedResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatedResource>> UploadPhoto(
        long id,
        [FromForm] TripPhotoRequest request,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        await using Stream? content = file?.OpenReadStream();
        FileUpload? upload = file is null ? null : new FileUpload(content!, file.FileName, file.ContentType, file.Length);

        CreatedResource created = await trips.AddPhotoAsync(id, request, upload, cancellationToken);
        return Created($"/{ApiRoutes.Base}/documents/{created.Id}/file", created);
    }

    /// <summary>Admin: approve the delivery proof. Completes the trip, approves the owner's payout and issues the GST invoice.</summary>
    [HttpPut("{id:long}/pod-approval")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApprovePod(long id, CancellationToken cancellationToken)
    {
        await trips.ApprovePodAsync(id, cancellationToken);
        return NoContent();
    }
}
