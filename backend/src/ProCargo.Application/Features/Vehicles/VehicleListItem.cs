using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Vehicles;

public sealed class VehicleListItem
{
    public long Id { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;

    public string VehicleType { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }

    public int CapacityKg { get; set; }

    public string? MakeModel { get; set; }

    public string AvailabilityStatus { get; set; } = string.Empty;

    public string VerificationStatus { get; set; } = string.Empty;

    public long? CurrentDriverId { get; set; }

    public string? DriverName { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>RC, insurance, permit... uploaded for this vehicle.</summary>
    public List<DocumentChip> Documents { get; set; } = [];
}
