namespace ProCargo.Application.Features.Bookings;

public sealed class PaymentSummary
{
    public string Method { get; set; } = string.Empty;

    public string Gateway { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? GatewayPaymentId { get; set; }
}
