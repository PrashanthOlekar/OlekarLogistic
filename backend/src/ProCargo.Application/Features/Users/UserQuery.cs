using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Users;

/// <summary>Filters for GET /users.</summary>
public sealed class UserQuery : ListQuery
{
    /// <summary>Customer, Owner, Driver or Admin.</summary>
    public string? Role { get; set; }

    /// <summary>Active, PendingKyc, Blocked or Closed.</summary>
    public string? Status { get; set; }

    /// <summary>Newest (default), Name or LastLogin.</summary>
    public string? Sort { get; set; }
}
