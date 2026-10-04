namespace ProCargo.Application.Features.Trips;

/// <summary>What the invoice needs, read from the database.</summary>
public sealed class PodApprovalData
{
    public long TripId { get; set; }

    public string Status { get; set; } = string.Empty;

    public long BookingId { get; set; }

    public long CustomerId { get; set; }

    public string? CustomerGstin { get; set; }

    public string? PickupState { get; set; }

    public string? DropState { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? TotalAmount { get; set; }
}
