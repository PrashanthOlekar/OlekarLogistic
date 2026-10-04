namespace ProCargo.Application.Features.Loads;

/// <param name="KycApproved">False until an admin approves the owner's KYC; the list is empty until then.</param>
public sealed record LoadsResponse(bool KycApproved, IReadOnlyList<AvailableLoad> Loads);
