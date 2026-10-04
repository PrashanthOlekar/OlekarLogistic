namespace ProCargo.Application.Features.ReferenceData;

/// <summary>Lists for the booking and registration forms.</summary>
public sealed class ReferenceDataResponse
{
    public required IReadOnlyList<CityOption> Cities { get; init; }

    public required IReadOnlyList<VehicleTypeOption> VehicleTypes { get; init; }

    public required IReadOnlyList<GoodsOption> Goods { get; init; }
}
