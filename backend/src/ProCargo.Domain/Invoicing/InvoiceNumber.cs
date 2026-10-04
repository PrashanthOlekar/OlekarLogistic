namespace ProCargo.Domain.Invoicing;

/// <summary>
/// Invoice numbers look like PC/26-27/000001: the Indian financial year (April–March)
/// and a running number from dbo.InvoiceNumberSeq.
/// </summary>
public static class InvoiceNumber
{
    public static string Format(long sequence, DateOnly indianDate)
    {
        int yearStart = indianDate.Month >= 4 ? indianDate.Year : indianDate.Year - 1;
        int yearEnd = yearStart + 1;

        return $"PC/{yearStart % 100:00}-{yearEnd % 100:00}/{sequence:000000}";
    }
}
