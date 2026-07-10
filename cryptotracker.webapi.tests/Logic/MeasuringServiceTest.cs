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
    private ExchangeIntegration _manualIntegration;

    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new DatabaseContext(options);
        _service = new MeasuringService(_db);

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

    private AddMeasuringDto Dto(decimal amount, DateTime? date = null) =>
        new() { Symbol = "BTC", Amount = amount, Date = date ?? DateTime.UtcNow };

    [Test]
    public async Task AddIntegrationMeasuring_CreatesMeasuring()
    {
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.5m));

        var measuring = await _db.AssetMeasurings.SingleAsync();
        Assert.That(measuring.Symbol, Is.EqualTo("BTC"));
        Assert.That(measuring.Amount, Is.EqualTo(0.5m));
        Assert.That(measuring.IntegrationId, Is.EqualTo(_manualIntegration.Id));
    }

    [Test]
    public async Task AddIntegrationMeasuring_SameDay_UpdatesExistingMeasuring()
    {
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.5m));
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.7m));

        var measuring = await _db.AssetMeasurings.SingleAsync();
        Assert.That(measuring.Amount, Is.EqualTo(0.7m));
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
        var dto = new AddMeasuringDto { Symbol = "UNKNOWN", Amount = 1m, Date = DateTime.UtcNow };

        Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, dto));
    }

    [Test]
    public async Task DeleteMeasuring_RemovesMeasuring()
    {
        await _service.AddIntegrationMeasuringAsync(_manualIntegration.Id, Dto(0.5m));
        var measuring = await _db.AssetMeasurings.SingleAsync();

        await _service.DeleteMeasuringAsync(measuring.Id);

        Assert.That(await _db.AssetMeasurings.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteMeasuring_NonManualIntegration_ThrowsInvalidOperationException()
    {
        var apiIntegration = new ExchangeIntegration { Name = "Api", IsManual = false };
        _db.ExchangeIntegrations.Add(apiIntegration);
        var measuring = new AssetMeasuring { Symbol = "BTC", IntegrationId = apiIntegration.Id, Timestamp = DateTime.UtcNow, Amount = 1m };
        _db.AssetMeasurings.Add(measuring);
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteMeasuringAsync(measuring.Id));
        Assert.That(await _db.AssetMeasurings.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task DeleteMeasuring_UnknownId_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteMeasuringAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetMeasuringsByIntegration_FiltersByIntegration()
    {
        var other = new ExchangeIntegration { Name = "Other", IsManual = true };
        _db.ExchangeIntegrations.Add(other);
        _db.AssetMeasurings.Add(new AssetMeasuring { Symbol = "BTC", IntegrationId = _manualIntegration.Id, Timestamp = DateTime.UtcNow, Amount = 1m });
        _db.AssetMeasurings.Add(new AssetMeasuring { Symbol = "BTC", IntegrationId = other.Id, Timestamp = DateTime.UtcNow, Amount = 2m });
        await _db.SaveChangesAsync();

        var result = await _service.GetMeasuringsByIntegrationAsync(_manualIntegration.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Single().Amount, Is.EqualTo(1m));
    }
}
