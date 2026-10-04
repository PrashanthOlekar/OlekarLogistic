namespace ProCargo.Domain.Entities;

/// <summary>A price offered for a booking (dbo.Quotes). The customer pays TotalAmount; the owner receives OwnerPayout.</summary>
public sealed class Quote
{
    public long QuoteId { get; set; }

    public long BookingId { get; set; }

    public decimal DistanceKm { get; set; }

    public decimal VehicleCost { get; set; }

    public decimal DriverCost { get; set; }

    public decimal LoadingCharges { get; set; }

    /// <summary>ProCargo's commission, taken from the owner's side.</summary>
    public decimal PlatformFee { get; set; }

    /// <summary>GST.</summary>
    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal OwnerPayout { get; set; }

    public DateTime ValidUntil { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? AcceptedAt { get; set; }
}
