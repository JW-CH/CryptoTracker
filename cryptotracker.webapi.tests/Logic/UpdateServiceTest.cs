using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Backgroundservices;
using cryptotracker.webapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class UpdateServiceTest
{
    private DatabaseContext _db;
    private Mock<IIntegrationProvider> _integrationProviderMock;
    private Mock<IPriceProvider> _cryptoProviderMock;
    private Mock<IPriceProvider> _currencyProviderMock;
    private AssetMetadataService _metadataService;
    private CryptoTrackerConfig _config;
    private PortfolioClock _clock;
    private UpdateService _service;

    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new DatabaseContext(options);

        _integrationProviderMock = new Mock<IIntegrationProvider>();
        _integrationProviderMock.Setup(x => x.Type).Returns(CryptoTrackerIntegrationType.Coinbase);

        // metadata import should be a no-op in these tests
        _cryptoProviderMock = new Mock<IPriceProvider>();
        _cryptoProviderMock.Setup(x => x.Handles).Returns(new[] { AssetType.Crypto });
        _cryptoProviderMock.Setup(x => x.GetAssetsAsync()).ReturnsAsync(new List<ProviderAsset>());
        _cryptoProviderMock.Setup(x => x.GetQuotesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(new List<AssetMetadata>());

        _currencyProviderMock = new Mock<IPriceProvider>();
        _currencyProviderMock.Setup(x => x.Handles).Returns(new[] { AssetType.Fiat });
        _currencyProviderMock.Setup(x => x.GetAssetsAsync()).ReturnsAsync(new List<ProviderAsset>());
        _currencyProviderMock.Setup(x => x.GetQuotesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(new List<AssetMetadata>());

        _config = new CryptoTrackerConfig { Interval = 60 };
        _clock = TestClock.Create();

        _metadataService = new AssetMetadataService(
            Mock.Of<ILogger<AssetMetadataService>>(),
            _db,
            new[] { _cryptoProviderMock.Object, _currencyProviderMock.Object },
            _config,
            _clock);
        _service = new UpdateService(Mock.Of<IServiceScopeFactory>(), Mock.Of<ILogger<UpdateService>>(), _config, _clock);

        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _service?.Dispose();
        _db?.Database.EnsureDeleted();
        _db?.Dispose();
    }

    private CryptoTrackerIntegration AddConfigIntegration(string name)
    {
        var integration = new CryptoTrackerIntegration { Name = name, Type = CryptoTrackerIntegrationType.Coinbase };
        _config.Integrations.Add(integration);
        return integration;
    }

    private void SetupBalances(string integrationName, params (string Symbol, decimal Balance)[] balances)
    {
        SetupBalanceResults(integrationName, balances.Select(b => new BalanceResult { Symbol = b.Symbol, Balance = b.Balance }).ToArray());
    }

    private void SetupBalanceResults(string integrationName, params BalanceResult[] balances)
    {
        _integrationProviderMock
            .Setup(x => x.GetBalancesAsync(It.Is<CryptoTrackerIntegration>(i => i.Name == integrationName)))
            .ReturnsAsync(balances.ToList());
    }

    private async Task<ExchangeIntegration> SeedIntegrationWithMeasurings(string name, DateTime timestamp, params (string Symbol, decimal Amount)[] measurings)
    {
        var integration = await _db.ExchangeIntegrations.FirstOrDefaultAsync(x => x.Name == name);
        if (integration == null)
        {
            integration = new ExchangeIntegration { Name = name };
            _db.ExchangeIntegrations.Add(integration);
        }

        foreach (var (symbol, amount) in measurings)
        {
            if (await _db.Assets.FindAsync(symbol) == null)
                _db.Assets.Add(new Asset { Symbol = symbol, AssetType = AssetType.Crypto, IsHidden = false });

            _db.AssetMeasurings.Add(new AssetMeasuring
            {
                Symbol = symbol,
                IntegrationId = integration.Id,
                Timestamp = timestamp,
                Amount = amount
            });
        }

        await _db.SaveChangesAsync();
        return integration;
    }

    private Task Import() => _service.Import(_db, new[] { _integrationProviderMock.Object }, _currencyProviderMock.Object, _metadataService);

    private Task<List<AssetMeasuring>> TodaysMeasurings() =>
        _db.AssetMeasurings.Where(m => m.Timestamp >= _clock.StartOfDayUtc(_clock.Today)).ToListAsync();

    [Test]
    public async Task Import_WritesMeasuringsAndCreatesAssetsAndIntegration()
    {
        AddConfigIntegration("A");
        SetupBalances("A", ("BTC", 0.5m), ("ETH", 2m));

        await Import();

        var today = await TodaysMeasurings();
        Assert.That(today, Has.Count.EqualTo(2));
        Assert.That(today.Single(m => m.Symbol == "BTC").Amount, Is.EqualTo(0.5m));
        Assert.That(today.Single(m => m.Symbol == "ETH").Amount, Is.EqualTo(2m));
        Assert.That(await _db.ExchangeIntegrations.CountAsync(x => x.Name == "A"), Is.EqualTo(1));
        Assert.That(await _db.Assets.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Import_DisappearedAsset_GetsSingleZeroMeasuring()
    {
        await SeedIntegrationWithMeasurings("A", TestClock.Now.UtcDateTime.AddDays(-1), ("BTC", 0.5m), ("ETH", 2m));
        AddConfigIntegration("A");
        SetupBalances("A", ("BTC", 0.6m)); // ETH no longer reported -> sold

        await Import();

        var today = await TodaysMeasurings();
        Assert.That(today, Has.Count.EqualTo(2));
        Assert.That(today.Single(m => m.Symbol == "BTC").Amount, Is.EqualTo(0.6m));
        Assert.That(today.Single(m => m.Symbol == "ETH").Amount, Is.EqualTo(0m));
    }

    [Test]
    public async Task Import_AssetAlreadyZeroInLastSnapshot_GetsNoFurtherZero()
    {
        await SeedIntegrationWithMeasurings("A", TestClock.Now.UtcDateTime.AddDays(-1), ("BTC", 0.5m), ("ETH", 0m));
        AddConfigIntegration("A");
        SetupBalances("A", ("BTC", 0.5m));

        await Import();

        var today = await TodaysMeasurings();
        Assert.That(today, Has.Count.EqualTo(1));
        Assert.That(today.Single().Symbol, Is.EqualTo("BTC"));
    }

    [Test]
    public async Task Import_NonZeroInOlderSnapshot_DoesNotResurrectZeroMeasuring()
    {
        // day -2 still had ETH, day -1 recorded the sale as 0; only the most
        // recent snapshot may be used as reference, so no new zero today
        await SeedIntegrationWithMeasurings("A", TestClock.Now.UtcDateTime.AddDays(-2), ("BTC", 0.5m), ("ETH", 2m));
        await SeedIntegrationWithMeasurings("A", TestClock.Now.UtcDateTime.AddDays(-1), ("BTC", 0.5m), ("ETH", 0m));
        AddConfigIntegration("A");
        SetupBalances("A", ("BTC", 0.5m));

        await Import();

        var today = await TodaysMeasurings();
        Assert.That(today, Has.Count.EqualTo(1));
        Assert.That(today.Single().Symbol, Is.EqualTo("BTC"));
    }

    [Test]
    public async Task Import_FailingIntegration_KeepsTodaysDataAndOthersContinue()
    {
        var seeded = await SeedIntegrationWithMeasurings("A", TestClock.Now.UtcDateTime, ("BTC", 0.7m));
        AddConfigIntegration("A");
        AddConfigIntegration("B");
        _integrationProviderMock
            .Setup(x => x.GetBalancesAsync(It.Is<CryptoTrackerIntegration>(i => i.Name == "A")))
            .ThrowsAsync(new InvalidOperationException("exchange down"));
        SetupBalances("B", ("XRP", 3m));

        await Import(); // must not throw

        var today = await TodaysMeasurings();
        // A failed after its data would have been cleared in the old code;
        // fetch-before-delete keeps today's existing measuring intact
        var aMeasurings = today.Where(m => m.IntegrationId == seeded.Id).ToList();
        Assert.That(aMeasurings, Has.Count.EqualTo(1));
        Assert.That(aMeasurings.Single().Amount, Is.EqualTo(0.7m));
        // B was imported despite A failing
        Assert.That(today.Count(m => m.Symbol == "XRP"), Is.EqualTo(1));
    }

    [Test]
    public async Task Import_BalanceWithFiatTypeHint_CreatesFiatAsset()
    {
        AddConfigIntegration("A");
        SetupBalanceResults("A", new BalanceResult { Symbol = "EUR", Balance = 100m, AssetType = AssetType.Fiat });

        await Import();

        var asset = await _db.Assets.FindAsync("EUR");
        Assert.That(asset, Is.Not.Null);
        Assert.That(asset!.AssetType, Is.EqualTo(AssetType.Fiat));
    }

    [Test]
    public async Task Import_NoTypeHint_KnownCurrencySymbol_CreatesFiatAsset()
    {
        _currencyProviderMock.Setup(x => x.GetAssetsAsync())
            .ReturnsAsync(new List<ProviderAsset> { new() { Symbol = "EUR", Name = "Euro", ExternalId = "EUR" } });
        AddConfigIntegration("A");
        SetupBalances("A", ("eur", 100m)); // no type hint, casing differs from currency list

        await Import();

        var asset = await _db.Assets.FindAsync("eur");
        Assert.That(asset, Is.Not.Null);
        Assert.That(asset!.AssetType, Is.EqualTo(AssetType.Fiat));
    }

    [Test]
    public async Task Import_NoTypeHint_UnknownSymbol_CreatesCryptoAsset()
    {
        _currencyProviderMock.Setup(x => x.GetAssetsAsync())
            .ReturnsAsync(new List<ProviderAsset> { new() { Symbol = "EUR", Name = "Euro", ExternalId = "EUR" } });
        AddConfigIntegration("A");
        SetupBalances("A", ("BTC", 0.5m));

        await Import();

        var asset = await _db.Assets.FindAsync("BTC");
        Assert.That(asset, Is.Not.Null);
        Assert.That(asset!.AssetType, Is.EqualTo(AssetType.Crypto));
    }

    [Test]
    public async Task Import_ExistingAsset_TypeIsNotOverwritten()
    {
        _db.Assets.Add(new Asset { Symbol = "EUR", AssetType = AssetType.Crypto, IsHidden = false });
        await _db.SaveChangesAsync();
        AddConfigIntegration("A");
        SetupBalanceResults("A", new BalanceResult { Symbol = "EUR", Balance = 100m, AssetType = AssetType.Fiat });

        await Import();

        var asset = await _db.Assets.FindAsync("EUR");
        Assert.That(asset!.AssetType, Is.EqualTo(AssetType.Crypto));
    }

    [Test]
    public async Task Import_NoTypeHint_CurrencyLookupFails_DefaultsToCrypto()
    {
        _currencyProviderMock.Setup(x => x.GetAssetsAsync())
            .ThrowsAsync(new HttpRequestException("frankfurter down"));
        AddConfigIntegration("A");
        SetupBalances("A", ("BTC", 0.5m));

        await Import(); // must not throw

        var asset = await _db.Assets.FindAsync("BTC");
        Assert.That(asset, Is.Not.Null);
        Assert.That(asset!.AssetType, Is.EqualTo(AssetType.Crypto));
        Assert.That(await TodaysMeasurings(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Import_IntegrationTypeWithoutProvider_IsSkippedAndOthersContinue()
    {
        _config.Integrations.Add(new CryptoTrackerIntegration { Name = "A", Type = CryptoTrackerIntegrationType.Cardano });
        AddConfigIntegration("B");
        SetupBalances("B", ("XRP", 3m));

        await Import(); // must not throw

        var today = await TodaysMeasurings();
        Assert.That(today, Has.Count.EqualTo(1));
        Assert.That(today.Single().Symbol, Is.EqualTo("XRP"));
    }

    [Test]
    public async Task Import_RunTwiceSameDay_ReplacesTodaysMeasurings()
    {
        AddConfigIntegration("A");
        SetupBalances("A", ("BTC", 0.5m));
        await Import();

        SetupBalances("A", ("BTC", 0.6m));
        await Import();

        var today = await TodaysMeasurings();
        Assert.That(today, Has.Count.EqualTo(1));
        Assert.That(today.Single().Amount, Is.EqualTo(0.6m));
    }
}
