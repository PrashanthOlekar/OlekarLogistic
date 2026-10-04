namespace ProCargo.Application.Features.Loads;

/// <summary>One of the owner's verified, available trucks.</summary>
public sealed class FreeVehicleRow
{
    public long Id { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }

    public int CapacityKg { get; set; }

    public long? CurrentDriverId { get; set; }
}
