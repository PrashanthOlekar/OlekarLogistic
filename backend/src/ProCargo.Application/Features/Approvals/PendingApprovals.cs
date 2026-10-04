namespace ProCargo.Application.Features.Approvals;

/// <summary>Everything waiting for an admin's KYC decision.</summary>
public sealed class PendingApprovals
{
    public required IReadOnlyList<PendingOwner> Owners { get; init; }

    public required IReadOnlyList<PendingDriver> Drivers { get; init; }

    public required IReadOnlyList<PendingVehicle> Vehicles { get; init; }
}
