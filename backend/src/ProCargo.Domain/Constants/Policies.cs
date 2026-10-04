namespace ProCargo.Domain.Constants;

/// <summary>Authorization policy names. Each one lists the roles it allows (see the API's AuthenticationExtensions).</summary>
public static class Policies
{
    public const string CustomerOnly = nameof(CustomerOnly);
    public const string OwnerOnly = nameof(OwnerOnly);
    public const string DriverOnly = nameof(DriverOnly);
    public const string AdminOnly = nameof(AdminOnly);

    /// <summary>Customers (their own bookings) and admins (all bookings).</summary>
    public const string BookingReaders = nameof(BookingReaders);

    /// <summary>Owners (their own fleet) and admins (everyone's).</summary>
    public const string FleetReaders = nameof(FleetReaders);

    /// <summary>Owners, drivers and admins: each sees the trips that concern them.</summary>
    public const string TripReaders = nameof(TripReaders);

    /// <summary>Owners take loads; admins assign trucks by hand.</summary>
    public const string TripAssigners = nameof(TripAssigners);

    /// <summary>Customers, owners and drivers upload their own documents.</summary>
    public const string DocumentUploaders = nameof(DocumentUploaders);
}
