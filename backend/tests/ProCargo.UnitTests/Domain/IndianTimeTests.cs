using ProCargo.Domain.Common;

namespace ProCargo.UnitTests.Domain;

public sealed class IndianTimeTests
{
    [Fact]
    public void Evening_utc_is_already_tomorrow_in_india()
    {
        var utc = new DateTime(2026, 10, 4, 19, 0, 0, DateTimeKind.Utc);   // 00:30 IST on 5 Oct

        Assert.Equal(new DateOnly(2026, 10, 5), IndianTime.TodayFor(utc));
    }

    [Fact]
    public void An_indian_day_starts_at_18_30_utc_the_day_before()
    {
        DateTime start = IndianTime.StartOfDayUtc(new DateOnly(2026, 10, 5));

        Assert.Equal(new DateTime(2026, 10, 4, 18, 30, 0, DateTimeKind.Utc), start);
        Assert.Equal(DateTimeKind.Utc, start.Kind);
    }
}
