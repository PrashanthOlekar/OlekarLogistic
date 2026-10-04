using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Messaging;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Features.Auth;
using ProCargo.Application.Features.Pricing;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.UnitTests.Services;

public sealed class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IOtpService _otp = Substitute.For<IOtpService>();
    private readonly ISessionIssuer _sessions = Substitute.For<ISessionIssuer>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();

    public AuthServiceTests()
    {
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns(call => "hash:" + call.Arg<string>());
    }

    private AuthService CreateService(bool showCode = false) => new(
        _users,
        _refreshTokens,
        _otp,
        Substitute.For<ISmsSender>(),
        _sessions,
        _tokens,
        Substitute.For<ICurrentUser>(),
        Options.Create(new OtpOptions { ShowCodeInResponse = showCode }),
        TestClock.Default,
        new SendOtpRequestValidator(),
        new LoginRequestValidator(),
        new RefreshTokenRequestValidator(),
        NullLogger<AuthService>.Instance);

    [Fact]
    public async Task Login_code_for_an_unknown_number_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().SendCodeAsync(new SendOtpRequest("9845012345", OtpPurpose.Login), CancellationToken.None));
    }

    [Fact]
    public async Task Signup_code_for_a_registered_number_is_a_conflict()
    {
        _users.GetByMobileAsync("9845012345", Arg.Any<CancellationToken>()).Returns(new User { UserId = 1 });

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().SendCodeAsync(new SendOtpRequest("+91 98450 12345", OtpPurpose.Signup), CancellationToken.None));
    }

    [Fact]
    public async Task The_code_is_only_returned_when_showing_codes_is_switched_on()
    {
        _otp.IssueAsync("9845012345", OtpPurpose.Signup, Arg.Any<CancellationToken>()).Returns("482915");

        SendOtpResponse hidden = await CreateService(showCode: false).SendCodeAsync(new SendOtpRequest("9845012345", OtpPurpose.Signup), CancellationToken.None);
        SendOtpResponse shown = await CreateService(showCode: true).SendCodeAsync(new SendOtpRequest("9845012345", OtpPurpose.Signup), CancellationToken.None);

        Assert.Null(hidden.DevCode);
        Assert.Equal("482915", shown.DevCode);
    }

    [Fact]
    public async Task Blocked_accounts_cannot_sign_in()
    {
        _users.GetByMobileAsync("9845012345", Arg.Any<CancellationToken>())
            .Returns(new User { UserId = 1, Status = UserStatus.Blocked, Role = Roles.Customer });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService().LoginAsync(new LoginRequest("9845012345", "123456"), CancellationToken.None));
        await _sessions.DidNotReceive().StartSessionAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_reused_refresh_token_ends_every_session_of_that_user()
    {
        _refreshTokens.GetByHashAsync("hash:stolen", Arg.Any<CancellationToken>()).Returns(new RefreshToken
        {
            UserId = 42,
            RevokedAt = TestClock.Default.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiresAt = TestClock.Default.GetUtcNow().UtcDateTime.AddDays(5),
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().RefreshAsync(new RefreshTokenRequest("stolen"), CancellationToken.None));
        await _refreshTokens.Received(1).RevokeAllForUserAsync(42, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_refresh_token_is_unauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().RefreshAsync(new RefreshTokenRequest("nope"), CancellationToken.None));
    }
}
