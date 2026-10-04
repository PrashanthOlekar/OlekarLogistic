namespace ProCargo.Domain.Entities;

/// <summary>The journey: one truck and one driver carrying one booking (dbo.Trips).</summary>
public sealed class Trip
{
    public long TripId { get; set; }

    public string TripNumber { get; set; } = string.Empty;

    public long BookingId { get; set; }

    public long OwnerId { get; set; }

    public long VehicleId { get; set; }

    public long DriverId { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>The sender's handover code, encrypted.</summary>
    public string? PickupOtpProtected { get; set; }

    /// <summary>The receiver's handover code, encrypted.</summary>
    public string? DeliveryOtpProtected { get; set; }

    public decimal? PlannedDistanceKm { get; set; }

    public decimal OwnerPayout { get; set; }

    public decimal? DriverPay { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}
