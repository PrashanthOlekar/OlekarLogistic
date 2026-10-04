namespace ProCargo.Domain.Constants;

/// <summary>Payments.Method values a customer can choose.</summary>
public static class PaymentMethods
{
    public static readonly IReadOnlyList<string> All = ["UPI", "CreditCard", "DebitCard", "NetBanking", "Wallet"];
}
