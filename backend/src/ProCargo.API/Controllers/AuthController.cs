using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProCargo.API.Configuration;
using ProCargo.Application.Features.Auth;

namespace ProCargo.API.Controllers;

/// <summary>
/// Sign-in with a mobile number and a one-time SMS code.
/// 1. POST /otp with purpose "Login"  2. POST /login with the code  3. send the accessToken as a Bearer token.
/// </summary>
[Route(ApiRoutes.Base + "/auth")]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    /// <summary>Send a one-time code by SMS. Purpose "Login" for existing accounts, "Signup" to register.</summary>
    /// <response code="200">Code sent. In Development the code is also returned as devCode.</response>
    /// <response code="404">Login requested for a number with no account.</response>
    /// <response code="409">Signup requested for a number that is already registered.</response>
    [HttpPost("otp")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(SendOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SendOtpResponse>> SendCode(SendOtpRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.SendCodeAsync(request, cancellationToken));

    /// <summary>Sign in with the code. Returns an access token, a refresh token and the profile.</summary>
    /// <response code="400">The code is wrong, expired or was tried too often.</response>
    /// <response code="403">The account is blocked.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));

    /// <summary>Swap a refresh token for new tokens. Each refresh token works once.</summary>
    /// <response code="401">The refresh token is unknown, expired or already used.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.RefreshAsync(request, cancellationToken));

    /// <summary>Sign out on this device: the refresh token stops working.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>The signed-in user's profile, with their customer, owner or driver details.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfile>> GetMyProfile(CancellationToken cancellationToken) =>
        Ok(await auth.GetProfileAsync(cancellationToken));
}
