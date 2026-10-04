using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions.Messaging;

namespace ProCargo.Infrastructure.Messaging;

/// <summary>
/// Development stand-in for SMS. In Development the code is written to the log so you can test;
/// elsewhere it is masked.
/// TODO before launch: replace with your DLT-registered SMS provider (MSG91, Twilio India...).
/// </summary>
internal sealed class LoggingSmsSender(IHostEnvironment environment, ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendOtpAsync(string mobile, string code, CancellationToken cancellationToken)
    {
        string shownCode = environment.IsDevelopment() ? code : "******";
        logger.LogInformation("SMS to mobile ending {MobileEnd}: sign-in code {Code}", mobile[^4..], shownCode);
        return Task.CompletedTask;
    }
}
