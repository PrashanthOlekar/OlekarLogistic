namespace ProCargo.Application.Abstractions.Messaging;

/// <summary>Sends sign-in codes by SMS. The development version only writes to the log.</summary>
public interface ISmsSender
{
    Task SendOtpAsync(string mobile, string code, CancellationToken cancellationToken);
}
