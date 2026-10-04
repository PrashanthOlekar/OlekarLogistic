namespace ProCargo.Domain.Entities;

/// <summary>A registered lorry (dbo.Vehicles). It must be Approved before it can take loads.</summary>
public sealed class Vehicle
{
    public long VehicleId { get; set; }

    public long OwnerId { get; set; }

    public int VehicleTypeId { get; set; }

    /// <summary>No spaces, for example KA01AB4521.</summary>
    public string RegistrationNumber { get; set; } = string.Empty;

    public int CapacityKg { get; set; }

    public string? MakeModel { get; set; }

    public short? ManufactureYear { get; set; }

    public string AvailabilityStatus { get; set; } = string.Empty;

    public string VerificationStatus { get; set; } = string.Empty;

    public long? CurrentDriverId { get; set; }

    public int? HomeCityId { get; set; }

    public DateTime CreatedAt { get; set; }
}
