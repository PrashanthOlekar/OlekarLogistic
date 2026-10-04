namespace ProCargo.Domain.Invoicing;

/// <summary>
/// How the GST on an invoice is split:
///   pickup and drop in the same state → half CGST + half SGST
///   different states                  → all IGST
/// </summary>
public sealed record GstSplit(decimal Cgst, decimal Sgst, decimal Igst)
{
    public static GstSplit For(decimal taxAmount, string? pickupState, string? dropState)
    {
        bool sameState = string.Equals(pickupState, dropState, StringComparison.OrdinalIgnoreCase);
        if (!sameState)
        {
            return new GstSplit(0, 0, taxAmount);
        }

        decimal cgst = Math.Round(taxAmount / 2, 2);
        return new GstSplit(cgst, taxAmount - cgst, 0);
    }
}
