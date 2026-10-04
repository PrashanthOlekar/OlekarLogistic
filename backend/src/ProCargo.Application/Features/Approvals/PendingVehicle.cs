using ProCargo.Application.Features.Common;

namespace ProCargo.Application.Features.Approvals;

public sealed class PendingVehicle
{
    public long Id { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;

    public string VehicleType { get; set; } = string.Empty;

    public int CapacityKg { get; set; }

    public string Owner { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<DocumentChip> Documents { get; set; } = [];
}
