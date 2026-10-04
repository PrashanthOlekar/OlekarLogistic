namespace ProCargo.Application.Features.ReferenceData;

public sealed class CityOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>The name in Kannada, when known.</summary>
    public string? NameKn { get; set; }

    public string State { get; set; } = string.Empty;
}
