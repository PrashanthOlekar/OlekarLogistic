using Microsoft.Extensions.Options;
using NSubstitute;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Features.Auth;
using ProCargo.Application.Features.Pricing;
using ProCargo.Domain.Entities;

namespace ProCargo.UnitTests.Services;

public sealed class OtpServiceTests
{
    private readonly IOtpRepository _codes = Substitute.For<IOtpRepository>();
    private readonly IBusinessSettingsProvider _settings = Substitute.For<IBusinessSettingsProvider>();

    public OtpServiceTests()
    {
        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new BusinessSettings(new Dictionary<string, string>()));
    }

    private OtpService CreateService() => new(_codes, _settings, Options.Create(new OtpOptions()), TestClock.Default);

    private void GivenCode(string code, byte attempts = 0) =>
        _codes.GetLatestActiveAsync("9845012345", "Login", Arg.Any<CancellationToken>()).Returns(new OtpCode
        {
            OtpCodeId = 5,
            CodeHash = OtpCodeHasher.Hash("9845012345", code),
            Attempts = attempts,
        });

    [Fact]
    public async Task Issued_codes_have_six_digits_and_only_their_hash_is_stored()
    {
        string code = await CreateService().IssueAsync("9845012345", "Login", CancellationToken.None);

        Assert.Matches("^[0-9]{6}$", code);
        await _codes.Received(1).CreateAsync(
            "9845012345", "Login", OtpCodeHasher.Hash("9845012345", code), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task More_than_five_codes_in_ten_minutes_is_too_many()
    {
        _codes.CountRecentAsync("9845012345", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(5);

        await Assert.ThrowsAsync<TooManyRequestsException>(() => CreateService().IssueAsync("9845012345", "Login", CancellationToken.None));
    }

    [Fact]
    public async Task A_wrong_code_counts_an_attempt()
    {
        GivenCode("123456");

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().VerifyAsync("9845012345", "654321", "Login", CancellationToken.None));
        await _codes.Received(1).RecordFailedAttemptAsync(5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task After_five_wrong_tries_even_the_right_code_is_refused()
    {
        GivenCode("123456", attempts: 5);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().VerifyAsync("9845012345", "123456", "Login", CancellationToken.None));
        Assert.Equal("Too many wrong tries. Request a new code.", error.Message);
    }

    [Fact]
    public async Task The_right_code_is_used_once()
    {
        GivenCode("123456");
        _codes.MarkUsedAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        await CreateService().VerifyAsync("9845012345", " 123456 ", "Login", CancellationToken.None);

        await _codes.Received(1).MarkUsedAsync(5, Arg.Any<CancellationToken>());
    }
}
