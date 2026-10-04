namespace ProCargo.Domain.Constants;

/// <summary>
/// Settlements.Status, in order:
/// AwaitingPod → Approved (POD checked) → Released (money sent to the owner).
/// </summary>
public static class SettlementStatus
{
    public const string AwaitingPod = "AwaitingPod";
    public const string Approved = "Approved";
    public const string Released = "Released";
    public const string OnHold = "OnHold";
}
