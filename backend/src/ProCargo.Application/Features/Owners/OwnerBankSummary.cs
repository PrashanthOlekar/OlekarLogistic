namespace ProCargo.Application.Features.Owners;

/// <summary>The payout account, without the full account number.</summary>
public sealed class OwnerBankSummary
{
    public string AccountHolder { get; set; } = string.Empty;

    public string AccountLast4 { get; set; } = string.Empty;

    public string Ifsc { get; set; } = string.Empty;

    public string? BankName { get; set; }

    public string PennyDropStatus { get; set; } = string.Empty;
}
