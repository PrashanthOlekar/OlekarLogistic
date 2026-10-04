using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Drivers;

/// <summary>Filters for GET /drivers.</summary>
public sealed class DriverQuery : ListQuery
{
    /// <summary>Pending, Approved or Rejected.</summary>
    public string? KycStatus { get; set; }

    /// <summary>Available, OnTrip or OffDuty.</summary>
    public string? DutyStatus { get; set; }

    /// <summary>Name (default) or Newest.</summary>
    public string? Sort { get; set; }
}
