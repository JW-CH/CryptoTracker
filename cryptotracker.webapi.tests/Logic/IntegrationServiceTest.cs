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

    private async Task<ExchangeIntegration> SeedIntegrationWithHolding(string name, decimal amount, decimal price = 100m)
    {
        var integration = new ExchangeIntegration { Name = name };
        _db.ExchangeIntegrations.Add(integration);

        if (!await _db.Assets.AnyAsync(x => x.Symbol == "BTC"))
        {
            _db.Assets.Add(new Asset { Symbol = "BTC", AssetType = AssetType.Crypto, IsHidden = false });
            _db.AssetPriceHistory.Add(new AssetPriceHistory
            {
                Symbol = "BTC",
                Date = _clock.Today,
                Currency = "chf",
                Price = price
            });
        }

        _db.DailyHoldings.Add(new DailyHolding
        {
            Symbol = "BTC",
            IntegrationId = integration.Id,
            Date = _clock.Today,
            Amount = amount,
            Source = HoldingSource.Sync
        });
        await _db.SaveChangesAsync();
        return integration;
    }

    [Test]
    public async Task GetIntegrations_ComputesCurrentValue()
    {
        var integration = await SeedIntegrationWithHolding("A", 1.5m, price: 100m);

        var result = await _service.GetIntegrationsAsync();

        Assert.That(result.Single(x => x.Id == integration.Id).CurrentValue, Is.EqualTo(150m));
    }

    [Test]
    public async Task GetIntegrationStandingByDays_SumsOnlyThatIntegration()
    {
        var integrationA = await SeedIntegrationWithHolding("A", 1m);
        await SeedIntegrationWithHolding("B", 2m);

        var result = await _service.GetIntegrationStandingByDaysAsync(integrationA.Id, 7);

        Assert.That(result, Has.Count.EqualTo(7));
        Assert.That(result[_clock.Today], Is.EqualTo(100m));
    }

    [Test]
    public async Task UpdateIntegration_RenamesManual()
    {
        var integration = new ExchangeIntegration { Name = "Old", IsManual = true };
        _db.ExchangeIntegrations.Add(integration);
        await _db.SaveChangesAsync();

        await _service.UpdateIntegrationAsync(integration.Id, new UpdateIntegrationDto { Name = "New", Description = "desc" });

        var updated = await _db.ExchangeIntegrations.SingleAsync(x => x.Id == integration.Id);
        Assert.That(updated.Name, Is.EqualTo("New"));
        Assert.That(updated.Description, Is.EqualTo("desc"));
    }

    [Test]
    public async Task UpdateIntegration_RejectsRenameOfAutomatic()
    {
        var integration = new ExchangeIntegration { Name = "Coinbase", IsManual = false };
        _db.ExchangeIntegrations.Add(integration);
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateIntegrationAsync(integration.Id, new UpdateIntegrationDto { Name = "Renamed" }));
    }

    [Test]
    public async Task UpdateIntegration_AllowsDescriptionChangeOnAutomatic()
    {
        var integration = new ExchangeIntegration { Name = "Coinbase", IsManual = false };
        _db.ExchangeIntegrations.Add(integration);
        await _db.SaveChangesAsync();

        await _service.UpdateIntegrationAsync(integration.Id, new UpdateIntegrationDto { Name = "Coinbase", Description = "main account" });

        var updated = await _db.ExchangeIntegrations.SingleAsync(x => x.Id == integration.Id);
        Assert.That(updated.Description, Is.EqualTo("main account"));
    }

    [Test]
    public async Task UpdateIntegration_RejectsDuplicateName()
    {
        _db.ExchangeIntegrations.Add(new ExchangeIntegration { Name = "Taken" });
        var integration = new ExchangeIntegration { Name = "Mine", IsManual = true };
        _db.ExchangeIntegrations.Add(integration);
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateIntegrationAsync(integration.Id, new UpdateIntegrationDto { Name = "taken" }));
    }

    [Test]
    public async Task DeleteIntegration_RemovesIntegrationAndHoldings()
    {
        var integration = await SeedIntegrationWithHolding("A", 1m);

        await _service.DeleteIntegrationAsync(integration.Id);

        Assert.That(await _db.ExchangeIntegrations.AnyAsync(x => x.Id == integration.Id), Is.False);
        Assert.That(await _db.DailyHoldings.AnyAsync(x => x.IntegrationId == integration.Id), Is.False);
    }
}
