namespace ProCargo.Api.Contracts;

// What the admin console sends to the /api/admin endpoints.

/// <summary>Approve or reject. A reason is required when rejecting.</summary>
public record ReviewRequest(bool Approve, string? Reason);

public record AssignTruckRequest(long VehicleId, long DriverId);

/// <summary>Utr: the bank transfer reference for the payout.</summary>
public record ReleasePayoutRequest(string? Utr);

public record BlockUserRequest(bool Block);
