namespace ProCargo.Domain.Entities;

/// <summary>A city we serve, with map coordinates for distance (dbo.Cities).</summary>
public sealed class City
{
    public int CityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? NameKn { get; set; }

    public string State { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public bool IsServiceable { get; set; }
}
