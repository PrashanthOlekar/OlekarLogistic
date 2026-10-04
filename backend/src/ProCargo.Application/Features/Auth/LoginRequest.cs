namespace ProCargo.Application.Features.Auth;

/// <summary>Sign in with the code sent by SMS.</summary>
public sealed record LoginRequest(string Mobile, string Code);
