namespace ProCargo.Application.Features.Auth;

/// <param name="Sent">True when the code was sent.</param>
/// <param name="DevCode">Development only: the code itself, so you can test without SMS. Always null in production.</param>
public sealed record SendOtpResponse(bool Sent, string? DevCode);
