namespace ProCargo.Domain.Constants;

/// <summary>Vehicles.AvailabilityStatus, set by the owner.</summary>
public static class VehicleAvailability
{
    public const string Available = "Available";
    public const string Busy = "Busy";
    public const string Maintenance = "Maintenance";

    public static readonly IReadOnlyList<string> All = [Available, Busy, Maintenance];
}
