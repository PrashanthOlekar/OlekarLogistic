namespace ProCargo.Api.Contracts;

// What the portal sends to the /api/owner endpoints.

public record AddVehicleRequest(
    string RegistrationNumber,
    int VehicleTypeId,
    int CapacityKg,
    string? MakeModel,
    short? ManufactureYear,
    int? HomeCityId);

/// <summary>Status: Available, Busy or Maintenance.</summary>
public record SetAvailabilityRequest(string Status);

/// <summary>DriverId null removes the regular driver.</summary>
public record SetRegularDriverRequest(long? DriverId);

public record AcceptLoadRequest(long VehicleId, long DriverId);
