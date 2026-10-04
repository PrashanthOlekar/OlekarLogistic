namespace ProCargo.Domain.Constants;

/// <summary>Users.Status</summary>
public static class UserStatus
{
    public const string Active = "Active";
    public const string PendingKyc = "PendingKyc";
    public const string Blocked = "Blocked";
    public const string Closed = "Closed";

    /// <summary>Accounts in these states are refused on every request, even with a valid token.</summary>
    public static bool IsLockedOut(string? status) => status is null or Blocked or Closed;
}
