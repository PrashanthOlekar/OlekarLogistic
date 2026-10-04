namespace ProCargo.Domain.Pricing;

/// <summary>The percentages from the Settings table that pricing needs.</summary>
/// <param name="CommissionPercent">ProCargo's share of the freight, taken from the owner's payout.</param>
/// <param name="GstPercent">GST on the freight, paid by the customer.</param>
public sealed record PricingRates(decimal CommissionPercent, decimal GstPercent);
