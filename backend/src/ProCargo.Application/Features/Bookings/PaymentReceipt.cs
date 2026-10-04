namespace ProCargo.Application.Features.Bookings;

/// <summary>The recorded payment.</summary>
public sealed class PaymentReceipt
{
    public long PaymentId { get; set; }

    public decimal Amount { get; set; }
}
