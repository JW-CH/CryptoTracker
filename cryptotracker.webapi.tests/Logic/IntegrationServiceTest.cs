using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using static cryptotracker.webapi.Services.IntegrationService;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class IntegrationServiceTest
{
    private DatabaseContext _db;
    private IntegrationService _service;
    private PortfolioClock _clock;

    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new DatabaseContext(options);

        var config = new CryptoTrackerConfig();
        _clock = TestClock.Create();
        _service = new IntegrationService(_db, new PortfolioQueryService(_db, config), _clock);

        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Database.EnsureDeleted();
        _db?.Dispose();
    }

    [Test]
    public async Task GetIntegrations_ReturnsAllIntegrations()
    {
        _db.ExchangeIntegrations.Add(new ExchangeIntegration { Name = "A" });
        _db.ExchangeIntegrations.Add(new ExchangeIntegration { Name = "B", IsHidden = true });
        await _db.SaveChangesAsync();

        var result = await _service.GetIntegrationsAsync();

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task AddIntegration_CreatesManualIntegration()
    {
        await _service.AddIntegrationAsync(new AddIntegrationDto { Name = "Manual", Description = "desc" });

        var integration = await _db.ExchangeIntegrations.SingleAsync(x => x.Name == "Manual");
        Assert.That(integration.IsManual, Is.True);
        Assert.That(integration.IsHidden, Is.False);
        Assert.That(integration.Description, Is.EqualTo("desc"));
    }

    [Test]
    public async Task AddIntegration_DuplicateNameIgnoringCase_ThrowsInvalidOperationException()
    {
        _db.ExchangeIntegrations.Add(new ExchangeIntegration { Name = "Coinbase" });
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddIntegrationAsync(new AddIntegrationDto { Name = "coinbase" }));
    }

    [Test]
    public async Task GetIntegrationDetails_UnknownId_ReturnsNull()
    {
        var result = await _service.GetIntegrationDetailsAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetIntegrationDetails_ReturnsIntegrationWithTodaysMeasurings()
    {
        var integration = new ExchangeIntegration { Name = "A" };
        _db.ExchangeIntegrations.Add(integration);
        _db.Assets.Add(new Asset { Symbol = "BTC", AssetType = AssetType.Crypto, IsHidden = false });

        _db.DailyHoldings.Add(new DailyHolding
        {
            Symbol = "BTC",
            IntegrationId = integration.Id,
            Date = _clock.Today,
            Amount = 1.5m,
            Source = HoldingSource.Sync
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetIntegrationDetailsAsync(integration.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Integration.Name, Is.EqualTo("A"));
        Assert.That(result.Value.Measurings, Has.Count.EqualTo(1));
        Assert.That(result.Value.Measurings.Single().TotalAmount, Is.EqualTo(1.5m));
    }
}
