using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A GST tax invoice. Table: Invoices.
/// </summary>
public class Invoice
{
    public long InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = "";

    public long BookingId { get; set; }

    public long CustomerId { get; set; }

    public string? CustomerGSTIN { get; set; }

    public string PlaceOfSupply { get; set; } = "";

    [Precision(12, 2)]
    public decimal TaxableAmount { get; set; }

    [Precision(12, 2)]
    public decimal CGST { get; set; }

    [Precision(12, 2)]
    public decimal SGST { get; set; }

    [Precision(12, 2)]
    public decimal IGST { get; set; }

    [Precision(12, 2)]
    public decimal TotalAmount { get; set; }

    public long? PdfDocumentId { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
