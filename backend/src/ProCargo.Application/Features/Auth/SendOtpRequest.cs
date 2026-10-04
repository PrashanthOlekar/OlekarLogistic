namespace ProCargo.Application.Features.Auth;

/// <summary>Ask for a one-time code by SMS.</summary>
/// <param name="Mobile">10-digit mobile number; "+91" and spaces are fine.</param>
/// <param name="Purpose">"Login" for an existing account, "Signup" to register.</param>
public sealed record SendOtpRequest(string Mobile, string Purpose);
