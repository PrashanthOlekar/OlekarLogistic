using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProCargo.API.Configuration;
using ProCargo.Application.Features.Auth;
using ProCargo.Application.Features.Registration;

namespace ProCargo.API.Controllers;

/// <summary>
/// New accounts. First POST /auth/otp with purpose "Signup", then send the code here.
/// The new user is signed in straight away. Owners and drivers start as PendingKyc until an admin approves them.
/// </summary>
[Route(ApiRoutes.Base + "/registrations")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public sealed class RegistrationsController(IRegistrationService registrations) : ApiControllerBase
{
    private const string ProfileLocation = "/" + ApiRoutes.Base + "/auth/me";

    /// <summary>Register a customer (company name and GSTIN optional).</summary>
    [HttpPost("customers")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> RegisterCustomer(RegisterCustomerRequest request, CancellationToken cancellationToken) =>
        Created(ProfileLocation, await registrations.RegisterCustomerAsync(request, cancellationToken));

    /// <summary>Register a lorry owner with PAN, Aadhaar last 4 digits and the payout bank account.</summary>
    [HttpPost("owners")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> RegisterOwner(RegisterOwnerRequest request, CancellationToken cancellationToken) =>
        Created(ProfileLocation, await registrations.RegisterOwnerAsync(request, cancellationToken));

    /// <summary>Register a driver, optionally linked to their owner by the owner's mobile number.</summary>
    [HttpPost("drivers")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> RegisterDriver(RegisterDriverRequest request, CancellationToken cancellationToken) =>
        Created(ProfileLocation, await registrations.RegisterDriverAsync(request, cancellationToken));
}
