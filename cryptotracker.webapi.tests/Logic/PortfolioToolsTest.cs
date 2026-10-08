using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Mcp;
using cryptotracker.webapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class PortfolioToolsTest
{
    private const string SecretMarker = "secret-marker";

    private DatabaseContext _db;
    private PortfolioClock _clock;
    private CryptoTrackerConfig _config;
    private PortfolioTools _tools;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new DatabaseContext(options);
        _clock = TestClock.Create();
        _config = new CryptoTrackerConfig
        {
            BaseCurrency = "chf",
            Timezone = "Europe/Zurich",
            Interval = 120,
            MaxFillDays = 10,
            ConnectionString = SecretMarker + "-connection",
            Auth = new CryptoTrackerAuth { Secret = SecretMarker + "-jwt" },
            Oidc = new CryptoTrackerOidc { ClientSecret = SecretMarker + "-oidc" },
            Mcp = new CryptoTrackerMcp { Enabled = true, Token = SecretMarker + "-mcp-token-32chars" },
            Integrations =
            [
                new CryptoTrackerIntegration
                {
                    Sources = [new CryptoTrackerIntegrationSource { Key = SecretMarker + "-key", Secret = SecretMarker + "-api", Passphrase = SecretMarker + "-pass" }]
                }
            ]
        };

        var portfolio = new PortfolioQueryService(_db, _config);
        var metadata = new AssetMetadataService(
            Mock.Of<ILogger<AssetMetadataService>>(),
            _db,
            Array.Empty<IPriceProvider>(),
            _config,
            _clock);
        _tools = new PortfolioTools(
            new IntegrationService(_db, portfolio, _clock),
            portfolio,
            new AssetService(_db, Array.Empty<IPriceProvider>(), _config, metadata),
            _clock,
            _config);
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Database.EnsureDeleted();
        _db?.Dispose();
    }

    [Test]
    public void GetContext_ReturnsOnlyNonSecretSettings()
    {
        var context = _tools.GetContext();

        Assert.That(
            typeof(PortfolioContext).GetProperties().Select(p => p.Name),
            Is.EquivalentTo(new[] { "BaseCurrency", "Timezone", "Today", "UpdateIntervalMinutes", "MaxFillDays" }));
        Assert.That(context.BaseCurrency, Is.EqualTo("chf"));
        Assert.That(context.Timezone, Is.EqualTo("Europe/Zurich"));
        Assert.That(context.Today, Is.EqualTo(_clock.Today));
        Assert.That(context.UpdateIntervalMinutes, Is.EqualTo(120));
        Assert.That(context.MaxFillDays, Is.EqualTo(10));
        Assert.That(context.ToString(), Does.Not.Contain(SecretMarker));
    }

    [Test]
    public void IsStale_UsesThreeIntervalsWithThreeHourFloor()
    {
        var now = _clock.UtcNow;

        Assert.That(PortfolioTools.IsStale(isManual: true, now.AddHours(-10), 120, now), Is.False);
        Assert.That(PortfolioTools.IsStale(isManual: false, lastSyncedAtUtc: null, 120, now), Is.False);
        Assert.That(PortfolioTools.IsStale(false, now.AddMinutes(-360), 120, now), Is.False);
        Assert.That(PortfolioTools.IsStale(false, now.AddMinutes(-361), 120, now), Is.True);
        Assert.That(PortfolioTools.IsStale(false, now.AddMinutes(-180), 10, now), Is.False);
        Assert.That(PortfolioTools.IsStale(false, now.AddMinutes(-181), 10, now), Is.True);
    }

    [Test]
    public async Task GetIntegrations_IncludesDescriptionAndStaleFlag()
    {
        await SeedHolding("Ledger", "BTC", 1m, price: 100m, description: "Hardware wallet", recordedAtUtc: _clock.UtcNow.AddHours(-10));
        await SeedHolding("Manual", "ETH", 2m, price: 50m, isManual: true, recordedAtUtc: _clock.UtcNow.AddDays(-30));

        var result = (await _tools.GetIntegrations()).ToList();

        var ledger = result.Single(i => i.Name == "Ledger");
        Assert.That(ledger.Description, Is.EqualTo("Hardware wallet"));
        Assert.That(ledger.IsStale, Is.True);
        Assert.That(ledger.CurrentValue, Is.EqualTo(100m));
        Assert.That(result.Single(i => i.Name == "Manual").IsStale, Is.False);
    }

    [Test]
    public void GetStanding_RejectsDaysOutOfRange()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.GetStanding(0));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.GetStanding(PortfolioTools.MaxDays + 1));
    }

    [Test]
    public void GetAssetSeries_RejectsDaysOutOfRange()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.GetAssetSeries("BTC", 0));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.GetAssetSeries("BTC", PortfolioTools.MaxDays + 1));
    }

    [Test]
    public void GetIntegrationStanding_RejectsDaysOutOfRange()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.GetIntegrationStanding("Ledger", 0));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.GetIntegrationStanding("Ledger", PortfolioTools.MaxDays + 1));
    }

    [Test]
    public void GetIntegrationStanding_UnknownName_Throws()
    {
        var error = Assert.ThrowsAsync<ArgumentException>(() => _tools.GetIntegrationStanding("missing"));

        Assert.That(error!.Message, Does.Contain("missing"));
    }

    [Test]
    public async Task GetIntegrationStanding_ResolvesNameIgnoringCase()
    {
        await SeedHolding("Ledger", "BTC", 1.5m, price: 100m);

        var result = await _tools.GetIntegrationStanding("  ledger  ", days: 1);

        Assert.That(result.Name, Is.EqualTo("Ledger"));
        Assert.That(result.BaseCurrency, Is.EqualTo("chf"));
        Assert.That(result.TotalsByDay[_clock.Today], Is.EqualTo(150m));
    }

    [Test]
    public async Task GetIntegrationStanding_AmbiguousName_Throws()
    {
        _db.ExchangeIntegrations.Add(new ExchangeIntegration { Name = "Ledger" });
        _db.ExchangeIntegrations.Add(new ExchangeIntegration { Name = "ledger" });
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<ArgumentException>(() => _tools.GetIntegrationStanding("LEDGER"));
    }

    [Test]
    public async Task GetHoldings_ProjectsLocationsShareAndHidesHiddenAssets()
    {
        await SeedHolding("Ledger", "BTC", 1m, price: 100m, assetName: "Bitcoin");
        await SeedHolding("Exchange", "BTC", 0.5m, price: 100m);
        await SeedHolding("Ledger", "ETH", 2m, price: 50m, assetName: "Ether");
        await SeedHolding("Ledger", "DOGE", 10m, price: 1m, hidden: true);

        var result = await _tools.GetHoldings();

        Assert.That(result.Date, Is.EqualTo(_clock.Today));
        Assert.That(result.Symbol, Is.Null);
        Assert.That(result.Integration, Is.Null);
        Assert.That(result.TotalValue, Is.EqualTo(250m));
        Assert.That(result.Holdings.Select(h => h.Symbol), Is.EqualTo(new[] { "BTC", "ETH" }));

        var btc = result.Holdings[0];
        Assert.That(btc.Name, Is.EqualTo("Bitcoin"));
        Assert.That(btc.AssetType, Is.EqualTo("Crypto"));
        Assert.That(btc.Amount, Is.EqualTo(1.5m));
        Assert.That(btc.Value, Is.EqualTo(150m));
        Assert.That(btc.Share, Is.EqualTo(0.6m));
        Assert.That(btc.Locations.Select(l => (l.Name, l.Amount, l.Value)), Is.EqualTo(new[] { ("Ledger", 1m, 100m), ("Exchange", 0.5m, 50m) }));

        var hidden = await _tools.GetHoldings(symbol: "doge");
        Assert.That(hidden.Symbol, Is.EqualTo("DOGE"));
        Assert.That(hidden.Holdings.Single().Amount, Is.EqualTo(10m));
    }

    [Test]
    public async Task GetHoldings_FiltersByIntegrationAndRejectsBadDates()
    {
        await SeedHolding("Ledger", "BTC", 1m, price: 100m);
        await SeedHolding("Exchange", "ETH", 2m, price: 50m);

        var ledger = await _tools.GetHoldings(integration: "ledger");

        Assert.That(ledger.Integration, Is.EqualTo("Ledger"));
        Assert.That(ledger.TotalValue, Is.EqualTo(100m));
        Assert.That(ledger.Holdings.Single().Symbol, Is.EqualTo("BTC"));

        Assert.ThrowsAsync<ArgumentException>(() => _tools.GetHoldings(date: "08.07.2026"));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _tools.GetHoldings(date: _clock.Today.AddDays(1).ToString("yyyy-MM-dd")));
    }

    [Test]
    public async Task GetAssetSeries_UnknownSymbolThrows_AndGapsAreZero()
    {
        await SeedHolding("Ledger", "BTC", 1m, price: 100m, assetName: "Bitcoin");

        Assert.ThrowsAsync<ArgumentException>(() => _tools.GetAssetSeries("missing"));

        var series = await _tools.GetAssetSeries("btc", days: 2);

        Assert.That(series.Symbol, Is.EqualTo("BTC"));
        Assert.That(series.Name, Is.EqualTo("Bitcoin"));
        Assert.That(series.Points.Select(p => p.Date), Is.EqualTo(new[] { _clock.Today.AddDays(-1), _clock.Today }));
        Assert.That(series.Points[0], Is.EqualTo(new AssetSeriesPoint(_clock.Today.AddDays(-1), 0m, 0m, 0m)));
        Assert.That(series.Points[1], Is.EqualTo(new AssetSeriesPoint(_clock.Today, 1m, 100m, 100m)));
    }

    [Test]
    public async Task GetAssets_IncludesHiddenAssetsWithoutPrices()
    {
        await SeedHolding("Ledger", "BTC", 1m, assetName: "Bitcoin");
        _db.Assets.Add(new Asset { Symbol = "DOGE", Name = "Dogecoin", AssetType = AssetType.Crypto, IsHidden = true });
        await _db.SaveChangesAsync();

        var assets = await _tools.GetAssets();

        Assert.That(assets.Select(a => (a.Symbol, a.Name, a.AssetType, a.IsHidden)), Is.EqualTo(new[]
        {
            ("BTC", "Bitcoin", "Crypto", false),
            ("DOGE", "Dogecoin", "Crypto", true)
        }));
    }

    private async Task SeedHolding(
        string integrationName,
        string symbol,
        decimal amount,
        decimal price = 100m,
        string? assetName = null,
        string? description = null,
        bool isManual = false,
        bool hidden = false,
        DateTime? recordedAtUtc = null)
    {
        var integration = await _db.ExchangeIntegrations.FirstOrDefaultAsync(x => x.Name == integrationName);
        if (integration == null)
        {
            integration = new ExchangeIntegration
            {
                Name = integrationName,
                Description = description,
                IsManual = isManual
            };
            _db.ExchangeIntegrations.Add(integration);
        }

        var asset = await _db.Assets.FirstOrDefaultAsync(x => x.Symbol == symbol);
        if (asset == null)
        {
            _db.Assets.Add(new Asset
            {
                Symbol = symbol,
                Name = assetName,
                AssetType = AssetType.Crypto,
                IsHidden = hidden
            });
            _db.AssetPriceHistory.Add(new AssetPriceHistory
            {
                Symbol = symbol,
                Date = _clock.Today,
                Currency = "chf",
                Price = price
            });
        }

        _db.DailyHoldings.Add(new DailyHolding
        {
            Symbol = symbol,
            IntegrationId = integration.Id,
            Date = _clock.Today,
            Amount = amount,
            Source = isManual ? HoldingSource.Manual : HoldingSource.Sync,
            RecordedAtUtc = recordedAtUtc ?? _clock.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
