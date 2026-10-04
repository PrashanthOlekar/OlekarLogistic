namespace ProCargo.Application.Features.Vehicles;

/// <summary>A lorry the owner wants to put on ProCargo. It starts as Pending until an admin approves it.</summary>
/// <param name="RegistrationNumber">As on the RC, for example "KA 01 AB 4521".</param>
/// <param name="CapacityKg">Load capacity in kg.</param>
public sealed record CreateVehicleRequest(
    string RegistrationNumber,
    int VehicleTypeId,
    int CapacityKg,
    string? MakeModel,
    short? ManufactureYear,
    int? HomeCityId);
