namespace ProCargo.Application.Features.Dashboards;

public sealed class RouteCount
{
    /// <summary>For example "Bengaluru → Hubballi".</summary>
    public string Route { get; set; } = string.Empty;

    public int Count { get; set; }
}
