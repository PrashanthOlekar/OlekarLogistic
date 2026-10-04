namespace ProCargo.Domain.Common;

/// <summary>
/// The database stores UTC. These helpers work out Indian dates (IST, UTC+5:30).
/// </summary>
public static class IndianTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromMinutes(330);

    /// <summary>The Indian wall-clock time for a UTC moment.</summary>
    public static DateTime FromUtc(DateTime utc) => utc.Add(Offset);

    /// <summary>The Indian calendar date for a UTC moment.</summary>
    public static DateOnly TodayFor(DateTime utc) => DateOnly.FromDateTime(FromUtc(utc));

    /// <summary>The UTC moment at which an Indian calendar day starts.</summary>
    public static DateTime StartOfDayUtc(DateOnly indianDate) =>
        DateTime.SpecifyKind(indianDate.ToDateTime(TimeOnly.MinValue) - Offset, DateTimeKind.Utc);
}
