using ProCargo.Domain.Invoicing;

namespace ProCargo.UnitTests.Domain;

public sealed class InvoicingTests
{
    [Fact]
    public void Same_state_splits_gst_into_cgst_and_sgst()
    {
        GstSplit split = GstSplit.For(870.55m, "Karnataka", "Karnataka");

        Assert.Equal(435.28m, split.Cgst);
        Assert.Equal(435.27m, split.Sgst);
        Assert.Equal(0m, split.Igst);
        Assert.Equal(870.55m, split.Cgst + split.Sgst);
    }

    [Fact]
    public void Different_states_charge_igst_only()
    {
        GstSplit split = GstSplit.For(900m, "Karnataka", "Maharashtra");

        Assert.Equal(0m, split.Cgst);
        Assert.Equal(0m, split.Sgst);
        Assert.Equal(900m, split.Igst);
    }

    [Theory]
    [InlineData(2026, 10, 4, 412, "PC/26-27/000412")]
    [InlineData(2027, 3, 31, 1, "PC/26-27/000001")]
    [InlineData(2027, 4, 1, 1, "PC/27-28/000001")]
    public void Invoice_number_follows_the_april_to_march_financial_year(int year, int month, int day, long sequence, string expected)
    {
        Assert.Equal(expected, InvoiceNumber.Format(sequence, new DateOnly(year, month, day)));
    }
}
