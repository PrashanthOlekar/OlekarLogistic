namespace ProCargo.Application.Features.Owners;

/// <summary>Totals for the owner's overview page.</summary>
public sealed class OwnerDashboard
{
    public long OwnerId { get; set; }

    public string? BusinessName { get; set; }

    public string KycStatus { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public int Vehicles { get; set; }

    public int VehiclesApproved { get; set; }

    public int Drivers { get; set; }

    public int ActiveTrips { get; set; }

    /// <summary>Payouts released this month (Indian calendar).</summary>
    public decimal EarnedThisMonth { get; set; }

    /// <summary>Payouts not released yet.</summary>
    public decimal PendingPayout { get; set; }

    public OwnerBankSummary? Bank { get; set; }
}
