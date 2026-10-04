namespace ProCargo.Application.Features.Payments;

public sealed class PaymentListItem
{
    public long Id { get; set; }

    public string BookingNumber { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;

    public string Gateway { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }

    public string? GatewayPaymentId { get; set; }

    public DateTime CreatedAt { get; set; }
}
