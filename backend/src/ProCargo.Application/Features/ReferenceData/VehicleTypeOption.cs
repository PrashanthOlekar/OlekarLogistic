namespace ProCargo.Application.Features.ReferenceData;

public sealed class VehicleTypeOption
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string BodyType { get; set; } = string.Empty;

    public int MaxLoadKg { get; set; }

    public decimal LengthFt { get; set; }

    public decimal WidthFt { get; set; }

    public decimal? HeightFt { get; set; }

    public string? RecommendedGoods { get; set; }
}
