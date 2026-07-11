using cryptotracker.core.Models;
using cryptotracker.webapi.Services;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class PortfolioClockTest
{
    [Test]
    public void Today_LateEveningUtcInSummer_IsNextZurichDay()
    {
        // 22:30 UTC = 00:30 Europe/Zurich (UTC+2) -> already the next portfolio day
        var clock = TestClock.Create(new DateTimeOffset(2026, 7, 8, 22, 30, 0, TimeSpan.Zero));

        Assert.That(clock.Today, Is.EqualTo(new DateOnly(2026, 7, 9)));
    }

    [Test]
    public void Today_LateEveningUtcInWinter_RespectsUtcPlusOne()
    {
        // winter is UTC+1: 22:30 UTC = 23:30 Zurich (same day), 23:30 UTC = 00:30 (next day)
        Assert.That(TestClock.Create(new DateTimeOffset(2026, 1, 8, 22, 30, 0, TimeSpan.Zero)).Today,
            Is.EqualTo(new DateOnly(2026, 1, 8)));
        Assert.That(TestClock.Create(new DateTimeOffset(2026, 1, 8, 23, 30, 0, TimeSpan.Zero)).Today,
            Is.EqualTo(new DateOnly(2026, 1, 9)));
    }

    [Test]
    public void StartOfDayUtc_ReturnsUtcInstantOfZurichMidnight()
    {
        var clock = TestClock.Create();

        var summer = clock.StartOfDayUtc(new DateOnly(2026, 7, 9));
        Assert.That(summer, Is.EqualTo(new DateTime(2026, 7, 8, 22, 0, 0, DateTimeKind.Utc)));
        Assert.That(summer.Kind, Is.EqualTo(DateTimeKind.Utc));

        var winter = clock.StartOfDayUtc(new DateOnly(2026, 1, 9));
        Assert.That(winter, Is.EqualTo(new DateTime(2026, 1, 8, 23, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void ToPortfolioDay_UnspecifiedKind_IsTreatedAsUtc()
    {
        var clock = TestClock.Create();
        var unspecified = new DateTime(2026, 7, 8, 22, 30, 0, DateTimeKind.Unspecified);

        Assert.That(clock.ToPortfolioDay(unspecified), Is.EqualTo(new DateOnly(2026, 7, 9)));
    }

    [Test]
    public void NormalizeUtc_UnspecifiedKind_BecomesUtcWithSameTicks()
    {
        var unspecified = new DateTime(2026, 7, 8, 22, 30, 0, DateTimeKind.Unspecified);

        var normalized = PortfolioClock.NormalizeUtc(unspecified);

        Assert.That(normalized.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(normalized.Ticks, Is.EqualTo(unspecified.Ticks));
    }

    [Test]
    public void UtcTimezone_TodayIsUtcDate()
    {
        var clock = TestClock.Create(new DateTimeOffset(2026, 7, 8, 23, 30, 0, TimeSpan.Zero), timezone: "UTC");

        Assert.That(clock.Today, Is.EqualTo(new DateOnly(2026, 7, 8)));
    }

    [Test]
    public void InvalidTimezone_ThrowsAtConstruction()
    {
        Assert.Throws<TimeZoneNotFoundException>(
            () => new PortfolioClock(TimeProvider.System, new CryptoTrackerConfig { Timezone = "Mars/OlympusMons" }));
    }
}
