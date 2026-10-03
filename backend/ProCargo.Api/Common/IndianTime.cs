namespace ProCargo.Api.Common;

/// <summary>
/// The database stores UTC. These helpers work out Indian dates (IST, UTC+5:30).
/// </summary>
public static class IndianTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(5.5);

    public static DateTime Now => DateTime.UtcNow.Add(Offset);

    public static DateOnly Today => DateOnly.FromDateTime(Now);

    /// <summary>Converts an IST date to the UTC time at which that day starts.</summary>
    public static DateTime StartOfDayUtc(DateTime istDate) => istDate.Date - Offset;
}
