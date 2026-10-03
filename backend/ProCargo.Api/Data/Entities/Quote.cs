using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// The price offered for a booking. Table: Quotes.
/// </summary>
public class Quote
{
    public long QuoteId { get; set; }

    public long BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    [Precision(8, 1)]
    public decimal DistanceKm { get; set; }

    [Precision(12, 2)]
    public decimal VehicleCost { get; set; }

    [Precision(12, 2)]
    public decimal DriverCost { get; set; }

    [Precision(12, 2)]
    public decimal LoadingCharges { get; set; }

    // ProCargo commission, deducted from the owner's payout
    [Precision(12, 2)]
    public decimal PlatformFee { get; set; }

    [Precision(12, 2)]
    public decimal TaxAmount { get; set; }

    // what the customer pays
    [Precision(12, 2)]
    public decimal TotalAmount { get; set; }

    // freight minus commission
    [Precision(12, 2)]
    public decimal OwnerPayout { get; set; }

    public DateTime ValidUntil { get; set; }

    // Draft | Sent | Accepted | Expired | Superseded | Rejected
    public string Status { get; set; } = "Sent";

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcceptedAt { get; set; }
}
