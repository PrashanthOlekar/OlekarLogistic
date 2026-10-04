namespace ProCargo.Application.Features.Dashboards;

/// <summary>Counters, work queues and chart data for the admin overview. "Today" and "month" follow the Indian calendar.</summary>
public sealed class AdminDashboard
{
    public int BookingsToday { get; set; }

    public int ActiveTrips { get; set; }

    public int CompletedThisMonth { get; set; }

    public int AwaitingPayment { get; set; }

    public int AwaitingTruck { get; set; }

    public decimal RevenueThisMonth { get; set; }

    public decimal CommissionThisMonth { get; set; }

    public int PendingApprovals { get; set; }

    public int PendingDocuments { get; set; }

    public int PodToApprove { get; set; }

    public int SettlementsToRelease { get; set; }

    /// <summary>Bookings created on each of the last 7 days, oldest first.</summary>
    public List<DayCount> Last7 { get; set; } = [];

    public List<RouteCount> Routes { get; set; } = [];

    public List<CityCount> Cities { get; set; } = [];

    /// <summary>Approved vehicles by availability.</summary>
    public List<StatusCount> Vehicles { get; set; } = [];
}
