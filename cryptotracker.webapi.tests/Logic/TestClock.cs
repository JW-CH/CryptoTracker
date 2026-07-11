using cryptotracker.core.Models;
using cryptotracker.webapi.Services;

namespace cryptotracker.webapi.tests.Logic;

/// <summary>Fixed-time TimeProvider — enough for services that only read "now".</summary>
public class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
    public override DateTimeOffset GetUtcNow() => UtcNow;
}

public static class TestClock
{
    // Wednesday noon UTC (14:00 in Europe/Zurich) — far away from any day boundary
    public static readonly DateTimeOffset Now = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);

    public static PortfolioClock Create(DateTimeOffset? now = null, string timezone = "Europe/Zurich")
        => new(new FixedTimeProvider(now ?? Now), new CryptoTrackerConfig { Timezone = timezone });
}
