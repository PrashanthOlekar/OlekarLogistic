using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A registered lorry. It must be Approved before it can take loads. Table: Vehicles.
/// </summary>
public class Vehicle
{
    public long VehicleId { get; set; }

    public long OwnerId { get; set; }

    public Owner Owner { get; set; } = null!;

    public int VehicleTypeId { get; set; }

    public VehicleType VehicleType { get; set; } = null!;

    public string RegistrationNumber { get; set; } = "";

    public int CapacityKg { get; set; }

    public string? MakeModel { get; set; }

    public short? ManufactureYear { get; set; }

    // Available | Busy | Maintenance
    public string AvailabilityStatus { get; set; } = "Available";

    // Pending | Approved | Rejected | Suspended
    public string VerificationStatus { get; set; } = "Pending";

    public long? CurrentDriverId { get; set; }

    public Driver? CurrentDriver { get; set; }

    public int? HomeCityId { get; set; }

    [Precision(9, 6)]
    public decimal? LastLatitude { get; set; }

    [Precision(9, 6)]
    public decimal? LastLongitude { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
