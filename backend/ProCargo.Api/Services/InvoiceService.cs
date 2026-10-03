using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;

namespace ProCargo.Api.Services;

/// <summary>
/// Issues the customer's GST invoice once a trip's delivery proof is approved.
///
///   Number:  PC/26-27/000001   (financial year April–March, running number from dbo.InvoiceNumberSeq)
///   Tax:     pickup and drop in the same state → half CGST + half SGST
///            different states                  → all IGST
/// </summary>
public class InvoiceService
{
    private readonly ProCargoDbContext _db;

    public InvoiceService(ProCargoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Adds the invoice for a booking. The booking must be loaded with its customer and both cities.
    /// The caller saves the changes.
    /// </summary>
    public async Task<Invoice> CreateForBookingAsync(Booking booking)
    {
        Quote quote = await _db.Quotes
            .Where(row => row.BookingId == booking.BookingId && row.Status == QuoteStatus.Accepted)
            .FirstAsync();

        string invoiceNumber = await NextInvoiceNumberAsync();
        bool sameState = booking.PickupCity?.State == booking.DropCity?.State;

        decimal cgst = 0;
        decimal sgst = 0;
        decimal igst = 0;
        if (sameState)
        {
            cgst = Math.Round(quote.TaxAmount / 2, 2);
            sgst = quote.TaxAmount - cgst;
        }
        else
        {
            igst = quote.TaxAmount;
        }

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            BookingId = booking.BookingId,
            CustomerId = booking.CustomerId,
            CustomerGSTIN = booking.Customer.GSTIN,
            PlaceOfSupply = booking.PickupCity?.State ?? "Karnataka",
            TaxableAmount = quote.TotalAmount - quote.TaxAmount,
            CGST = cgst,
            SGST = sgst,
            IGST = igst,
            TotalAmount = quote.TotalAmount,
        };
        _db.Invoices.Add(invoice);

        return invoice;
    }

    private async Task<string> NextInvoiceNumberAsync()
    {
        List<long> values = await _db.Database
            .SqlQueryRaw<long>("SELECT CAST(NEXT VALUE FOR dbo.InvoiceNumberSeq AS BIGINT) AS [Value]")
            .ToListAsync();
        long sequence = values.Single();

        // The Indian financial year starts on 1 April.
        DateTime now = IndianTime.Now;
        int yearStart = now.Month >= 4 ? now.Year : now.Year - 1;
        int yearEnd = yearStart + 1;

        return $"PC/{yearStart % 100:00}-{yearEnd % 100:00}/{sequence:000000}";
    }
}
