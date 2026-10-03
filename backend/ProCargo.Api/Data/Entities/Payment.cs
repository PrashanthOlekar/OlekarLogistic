using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// Money received from a customer. Table: Payments.
/// </summary>
public class Payment
{
    public long PaymentId { get; set; }

    public long BookingId { get; set; }

    public long CustomerId { get; set; }

    public long QuoteId { get; set; }

    [Precision(12, 2)]
    public decimal Amount { get; set; }

    // UPI | CreditCard | DebitCard | NetBanking | Wallet | Credit
    public string Method { get; set; } = "UPI";

    public string Gateway { get; set; } = "Test";

    public string? GatewayOrderId { get; set; }

    public string? GatewayPaymentId { get; set; }

    // Created | Authorized | Captured | Failed | Refunded | PartiallyRefunded
    public string Status { get; set; } = "Created";

    public string? FailureReason { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
