namespace ProCargo.UnitTests;

/// <summary>A clock fixed at a known moment, so date rules are predictable.</summary>
internal sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    /// <summary>10:00 IST on 4 October 2026.</summary>
    public static TestClock Default { get; } = new(new DateTimeOffset(2026, 10, 4, 4, 30, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => now;
}
