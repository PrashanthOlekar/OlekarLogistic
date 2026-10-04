namespace ProCargo.Application.Features.Auth;

/// <summary>Issues and checks one-time sign-in codes. Shared by sign-in and registration.</summary>
public interface IOtpService
{
    /// <summary>Creates a code and returns it, so the caller can send it.</summary>
    Task<string> IssueAsync(string mobile, string purpose, CancellationToken cancellationToken);

    /// <summary>Checks a code. Throws a friendly error if it is wrong, expired or tried too often.</summary>
    Task VerifyAsync(string mobile, string code, string purpose, CancellationToken cancellationToken);
}
