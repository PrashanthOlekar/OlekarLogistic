using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A vehicle class customers choose from, with its rate card. Table: VehicleTypes.
/// </summary>
public class VehicleType
{
    public int VehicleTypeId { get; set; }

    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public string BodyType { get; set; } = "Closed";

    public int MaxLoadKg { get; set; }

    [Precision(5, 1)]
    public decimal LengthFt { get; set; }

    [Precision(5, 1)]
    public decimal WidthFt { get; set; }

    [Precision(5, 1)]
    public decimal? HeightFt { get; set; }

    public string? RecommendedGoods { get; set; }

    [Precision(8, 2)]
    public decimal RatePerKm { get; set; }

    [Precision(10, 2)]
    public decimal MinFare { get; set; }

    [Precision(8, 2)]
    public decimal DriverBattaPerDay { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
