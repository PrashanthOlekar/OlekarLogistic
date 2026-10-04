namespace ProCargo.Application.Features.Loads;

public sealed record AvailableLoadsData(IReadOnlyList<LoadRow> Loads, IReadOnlyList<FreeVehicleRow> Vehicles);
