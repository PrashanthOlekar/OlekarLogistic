namespace ProCargo.Application.Features.Settlements;

/// <summary>Record a payout already sent from the bank.</summary>
/// <param name="Utr">The bank's transfer reference number.</param>
public sealed record RecordPayoutRequest(string Utr);
