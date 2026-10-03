using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A city we serve, with map coordinates for distance. Table: Cities.
/// </summary>
public class City
{
    public int CityId { get; set; }

    public string Name { get; set; } = "";

    public string? NameKn { get; set; }

    public string State { get; set; } = "";

    [Precision(9, 6)]
    public decimal Latitude { get; set; }

    [Precision(9, 6)]
    public decimal Longitude { get; set; }

    public bool IsServiceable { get; set; } = true;
}
