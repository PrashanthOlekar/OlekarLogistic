namespace ProCargo.Application.Features.Common;

/// <summary>An admin's decision on an owner, driver, vehicle or document.</summary>
/// <param name="Approve">True to approve, false to reject.</param>
/// <param name="Reason">Required when rejecting, so the applicant knows what to fix.</param>
public sealed record VerificationRequest(bool Approve, string? Reason);
