using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// An owner's response to a paid load (accepted or declined). Table: LoadOffers.
/// </summary>
public class LoadOffer
{
    public long LoadOfferId { get; set; }

    public long BookingId { get; set; }

    public long OwnerId { get; set; }

    public long? VehicleId { get; set; }

    [Precision(12, 2)]
    public decimal OfferedPayout { get; set; }

    // Offered | Accepted | Declined | Expired | Withdrawn
    public string Status { get; set; } = "Offered";

    public DateTime ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
