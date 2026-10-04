namespace ProCargo.Domain.Entities;

/// <summary>A vehicle class customers choose from, with its rate card (dbo.VehicleTypes).</summary>
public sealed class VehicleType
{
    public int VehicleTypeId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string BodyType { get; set; } = string.Empty;

    public int MaxLoadKg { get; set; }

    public decimal LengthFt { get; set; }

    public decimal WidthFt { get; set; }

    public decimal? HeightFt { get; set; }

    public string? RecommendedGoods { get; set; }

    /// <summary>₹ per km.</summary>
    public decimal RatePerKm { get; set; }

    /// <summary>₹ minimum for short trips.</summary>
    public decimal MinFare { get; set; }

    /// <summary>₹ driver allowance per day.</summary>
    public decimal DriverBattaPerDay { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }
}
