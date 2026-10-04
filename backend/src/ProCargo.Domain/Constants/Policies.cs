namespace ProCargo.Domain.Constants;

/// <summary>
/// Authorization policy names. Each one allows the roles in its name
/// (registered in the API's AuthenticationExtensions). Services then narrow
/// the data to the caller's own records, e.g. an owner only sees their own trucks.
/// </summary>
public static class Policies
{
    public const string CustomerOnly = nameof(CustomerOnly);
    public const string OwnerOnly = nameof(OwnerOnly);
    public const string DriverOnly = nameof(DriverOnly);
    public const string AdminOnly = nameof(AdminOnly);

    /// <summary>Customers (their own bookings) and admins (all).</summary>
    public const string CustomerOrAdmin = nameof(CustomerOrAdmin);

    /// <summary>Owners (their own fleet, trips and payouts) and admins (all).</summary>
    public const string OwnerOrAdmin = nameof(OwnerOrAdmin);

    /// <summary>Owners, drivers and admins: each sees the trips that concern them.</summary>
    public const string OwnerDriverOrAdmin = nameof(OwnerDriverOrAdmin);

    /// <summary>Customers, owners and drivers: people who upload documents for their own profile.</summary>
    public const string AccountHolder = nameof(AccountHolder);
}
