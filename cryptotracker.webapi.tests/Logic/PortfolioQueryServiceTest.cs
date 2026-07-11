using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class PortfolioQueryServiceTest
{
    private const int MaxFillDays = 10;

    private DatabaseContext _db;
    private PortfolioQueryService _service;
    private PortfolioClock _clock;
    private ExchangeIntegration _integrationA;
    private ExchangeIntegration _integrationB;
    private DateOnly _today;

    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new DatabaseContext(options);
        _clock = TestClock.Create();
        _service = new PortfolioQueryService(_db, new CryptoTrackerConfig { MaxFillDays = MaxFillDays }, _clock);
        _today = _clock.Today;

        _integrationA = new ExchangeIntegration { Name = "Exchange A" };
        _integrationB = new ExchangeIntegration { Name = "Exchange B" };
        _db.ExchangeIntegrations.AddRange(_integrationA, _integrationB);

        _db.Assets.Add(new Asset { Symbol = "BTC", AssetType = AssetType.Crypto, IsHidden = false });
        _db.AssetPriceHistory.Add(new AssetPriceHistory
        {
            Symbol = "BTC",
            Date = _today,
            Currency = "chf",
            Price = 100m
        });

        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Database.EnsureDeleted();
        _db?.Dispose();
    }

    private async Task AddMeasuring(DateOnly day, decimal amount, ExchangeIntegration? integration = null)
    {
        _db.AssetMeasurings.Add(new AssetMeasuring
        {
            Symbol = "BTC",
            IntegrationId = (integration ?? _integrationA).Id,
            // noon keeps the measuring safely inside the day regardless of test runtime
            Timestamp = day.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
            Amount = amount
        });
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task MeasuringLateEveningUtc_CountsTowardsNextPortfolioDay()
    {
        // 22:30 UTC on day-1 is already "today" in Europe/Zurich (summer, UTC+2)
        _db.AssetMeasurings.Add(new AssetMeasuring
        {
            Symbol = "BTC",
            IntegrationId = _integrationA.Id,
            Timestamp = _clock.StartOfDayUtc(_today).AddMinutes(30),
            Amount = 0.5m
        });
        await _db.SaveChangesAsync();

        var yesterday = await _service.GetAssetDayMeasuringAsync(_today.AddDays(-1));
        var today = await _service.GetAssetDayMeasuringAsync(_today);

        Assert.That(yesterday, Is.Empty);
        Assert.That(today, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GapWithinFillLimit_CarriesLastMeasuringForward()
    {
        await AddMeasuring(_today.AddDays(-3), 0.5m);

        var result = await _service.GetAssetDayMeasuringAsync(_today);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].TotalAmount, Is.EqualTo(0.5m));
        Assert.That(result[0].TotalValue, Is.EqualTo(50m));
    }

    [Test]
    public async Task MeasuringExactlyAtFillLimit_IsStillUsed()
    {
        await AddMeasuring(_today.AddDays(-MaxFillDays), 0.5m);

        var result = await _service.GetAssetDayMeasuringAsync(_today);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GapBeyondFillLimit_TreatsDataAsMissing()
    {
        await AddMeasuring(_today.AddDays(-(MaxFillDays + 1)), 0.5m);

        var result = await _service.GetAssetDayMeasuringAsync(_today);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ZeroMeasuring_EndsForwardFill()
    {
        await AddMeasuring(_today.AddDays(-5), 0.5m);
        await AddMeasuring(_today.AddDays(-2), 0m); // sold

        var result = await _service.GetAssetDayMeasuringAsync(_today);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task DayBeforeSale_StillShowsHolding()
    {
        await AddMeasuring(_today.AddDays(-5), 0.5m);
        await AddMeasuring(_today.AddDays(-2), 0m); // sold

        var result = await _service.GetAssetDayMeasuringAsync(_today.AddDays(-3));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].TotalAmount, Is.EqualTo(0.5m));
    }

    [Test]
    public async Task ZeroInOneIntegration_KeepsHoldingOfOtherIntegration()
    {
        await AddMeasuring(_today.AddDays(-1), 0m, _integrationA);  // sold on A
        await AddMeasuring(_today.AddDays(-1), 2m, _integrationB);  // still held on B

        var result = await _service.GetAssetDayMeasuringAsync(_today);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].TotalAmount, Is.EqualTo(2m));
    }

    [Test]
    public async Task BatchQuery_ShowsHoldingUntilSaleAndNothingAfter()
    {
        await AddMeasuring(_today.AddDays(-4), 0.5m);
        await AddMeasuring(_today.AddDays(-2), 0m); // sold

        var days = Enumerable.Range(0, 5).Select(i => _today.AddDays(-i)).ToList();
        var result = await _service.GetAssetDayMeasuringBatchAsync(days);

        Assert.That(result[_today.AddDays(-4)], Has.Count.EqualTo(1));
        Assert.That(result[_today.AddDays(-3)], Has.Count.EqualTo(1));
        Assert.That(result[_today.AddDays(-2)], Is.Empty);
        Assert.That(result[_today.AddDays(-1)], Is.Empty);
        Assert.That(result[_today], Is.Empty);
    }
}
