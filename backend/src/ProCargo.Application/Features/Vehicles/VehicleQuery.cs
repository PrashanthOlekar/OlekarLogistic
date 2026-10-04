using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Vehicles;

/// <summary>Filters for GET /vehicles.</summary>
public sealed class VehicleQuery : ListQuery
{
    public int? VehicleTypeId { get; set; }

    /// <summary>Available, Busy or Maintenance.</summary>
    public string? AvailabilityStatus { get; set; }

    /// <summary>Pending, Approved, Rejected or Suspended.</summary>
    public string? VerificationStatus { get; set; }

    /// <summary>Registration (default) or Newest.</summary>
    public string? Sort { get; set; }
}
