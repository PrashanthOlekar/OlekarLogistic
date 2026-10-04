namespace ProCargo.Application.Features.Bookings;

/// <summary>The customer's GST invoice, issued when the delivery proof is approved.</summary>
public sealed class InvoiceSummary
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal TaxableAmount { get; set; }

    public decimal Cgst { get; set; }

    public decimal Sgst { get; set; }

    public decimal Igst { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime IssuedAt { get; set; }
}
