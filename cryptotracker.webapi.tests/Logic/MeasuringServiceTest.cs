using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using static cryptotracker.webapi.Services.MeasuringService;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class MeasuringServiceTest
{
    private DatabaseContext _db;
    private MeasuringService _service;
    private PortfolioClock _clock;
    private ExchangeIntegration _manualIntegration;
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
        _today = _clock.Today;
        _service = new MeasuringService(_db, _clock);

        _manualIntegration = new ExchangeIntegration { Name = "Manual", IsManual = true };
        _db.ExchangeIntegrations.Add(_manualIntegration);
        _db.Assets.Add(new Asset { Symbol = "BTC", AssetType = AssetType.Crypto, IsHidden = false });
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Database.EnsureDeleted();
        _db?.Dispose();
    }

    private AddMeasuringDto Dto(decimal amount, DateOnly? date = null) =>
        new() { Symbol = "BTC", Amount = amount, Date = date ?? _today };

    [Test]
    public async Task AddIntegrationMeasuring_CreatesManualHolding()
    {
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.5m));

        var holding = await _db.DailyHoldings.SingleAsync();
        Assert.That(holding.Symbol, Is.EqualTo("BTC"));
        Assert.That(holding.Amount, Is.EqualTo(0.5m));
        Assert.That(holding.IntegrationId, Is.EqualTo(_manualIntegration.Id));
        Assert.That(holding.Date, Is.EqualTo(_today));
        Assert.That(holding.Source, Is.EqualTo(HoldingSource.Manual));
        Assert.That(holding.RecordedAtUtc, Is.EqualTo(_clock.UtcNow));
    }

    [Test]
    public async Task AddIntegrationMeasuring_SameDay_UpdatesExistingHolding()
    {
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.5m));
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.7m));

        var holding = await _db.DailyHoldings.SingleAsync();
        Assert.That(holding.Amount, Is.EqualTo(0.7m));
    }

    [Test]
    public async Task AddIntegrationMeasuring_DifferentDays_CreatesSeparateHoldings()
    {
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.5m, _today.AddDays(-1)));
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.7m, _today));

        Assert.That(await _db.DailyHoldings.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task AddIntegrationMeasuring_UnknownIntegration_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddIntegrationMeasuringAsync(Guid.NewGuid(), Dto(0.5m)));
    }

    [Test]
    public async Task AddIntegrationMeasuring_NonManualIntegration_ThrowsInvalidOperationException()
    {
        var apiIntegration = new ExchangeIntegration { Name = "Api", IsManual = false };
        _db.ExchangeIntegrations.Add(apiIntegration);
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddIntegrationMeasuringAsync(apiIntegration.Id, Dto(0.5m)));
    }

    [Test]
    public async Task AddIntegrationMeasuring_UnknownAsset_ThrowsKeyNotFoundException()
    {
        var dto = new AddMeasuringDto { Symbol = "UNKNOWN", Amount = 1m, Date = _today };

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, dto));
    }

    [Test]
    public async Task DeleteMeasuring_RemovesHolding()
    {
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.5m));

        await _service.DeleteMeasuringAsync(_manualIntegration.Id, "BTC", _today);

        Assert.That(await _db.DailyHoldings.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteMeasuring_NonManualIntegration_ThrowsInvalidOperationException()
    {
        var apiIntegration = new ExchangeIntegration { Name = "Api", IsManual = false };
        _db.ExchangeIntegrations.Add(apiIntegration);
        _db.DailyHoldings.Add(new DailyHolding { Symbol = "BTC", IntegrationId = apiIntegration.Id, Date = _today, Amount = 1m, Source = HoldingSource.Sync });
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteMeasuringAsync(apiIntegration.Id, "BTC", _today));
        Assert.That(await _db.DailyHoldings.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task DeleteMeasuring_UnknownKey_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteMeasuringAsync(Guid.NewGuid(), "BTC", _today));
    }

    [Test]
    public async Task GetMeasuringsByIntegration_FiltersByIntegration()
    {
        var other = new ExchangeIntegration { Name = "Other", IsManual = true };
        _db.ExchangeIntegrations.Add(other);
        _db.DailyHoldings.Add(new DailyHolding { Symbol = "BTC", IntegrationId = _manualIntegration.Id, Date = _today, Amount = 1m, Source = HoldingSource.Manual });
        _db.DailyHoldings.Add(new DailyHolding { Symbol = "BTC", IntegrationId = other.Id, Date = _today, Amount = 2m, Source = HoldingSource.Manual });
        await _db.SaveChangesAsync();

        var result = await _service.GetMeasuringsByIntegrationAsync(_manualIntegration.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Single().Amount, Is.EqualTo(1m));
    }
}
