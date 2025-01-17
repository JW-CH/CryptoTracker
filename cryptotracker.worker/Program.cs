using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;


var root = Directory.GetCurrentDirectory();
string ymlConfigPath;

// #if DEBUG
// ymlConfigPath = Path.Combine(root, "..", "..", "..", "..", "config", "config.yml");
// #else
// ymlConfigPath = Path.Combine(root, "config", "config.yml");
// #endif

if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    ymlConfigPath = Path.Combine(root, "config", "config.yml");
}
else
{
    ymlConfigPath = Path.Combine(root, "..", "..", "..", "..", "config", "config.yml");
}

CryptotrackerConfig config;

if (File.Exists(ymlConfigPath))
{
    var yml = File.ReadAllText(ymlConfigPath);

    config = CryptotrackerConfig.LoadFromYml(yml);
}
else
{
    throw new Exception("Config file not found");
}
var loglevelNotLoaded = false;
// Create a LoggerFactory
using var loggerFactory = LoggerFactory.Create(builder =>
{
    LogLevel level;

    if (string.IsNullOrWhiteSpace(config.LogLevel) || !Enum.TryParse(config.LogLevel, true, out level))
    {
        if (!string.IsNullOrWhiteSpace(config.LogLevel))
            loglevelNotLoaded = true;
        level = LogLevel.Information;
    }

    _ = builder.AddSimpleConsole(options =>
                {
                    // Customizing the log output format
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";  // Custom timestamp format
                    options.SingleLine = true;
                })
            .SetMinimumLevel(level); // Set minimum log level
});

// Create a logger instance
var logger = loggerFactory.CreateLogger("Cryptotracker");

logger.LogInformation("Config loaded");

logger.LogInformation($"LogLevel: '{config.LogLevel}'");
if (loglevelNotLoaded)
{
    logger.LogWarning($"LogLevel '{config.LogLevel}' is not valid, using '{nameof(LogLevel.Information)}' instead!");
}

logger.LogInformation($"Integrations: {config.Integrations.Count}");

var cryptoTrackerLogic = new CryptoTrackerLogic(logger);

var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
optionsBuilder.UseMySQL(config.ConnectionString);

// Apply migrations
using (var db = new DatabaseContext(optionsBuilder.Options))
{
    db.Database.Migrate();
}

while (true)
{
    logger.LogInformation("Starting import");

    await Import();
    logger.LogInformation("Import finished");

    logger.LogInformation($"Waiting {config.Interval} minutes");
    await Task.Delay(1000 * 60 * config.Interval);
}

async Task Import()
{
    using var db = new DatabaseContext(optionsBuilder.Options);

    logger.LogTrace("Clearing today's AssetMeasurings entries");
    var entries = db.AssetMeasurings.Where(x => x.Timestamp.Date == DateTime.Today.Date);
    var count = entries.Count();
    db.AssetMeasurings.RemoveRange(entries);
    logger.LogTrace($"Removed {count} AssetMeasurings");
    //db.Assets.RemoveRange(db.Assets);
    //db.ExchangeIntegrations.RemoveRange(db.ExchangeIntegrations);

    db.SaveChanges();
    logger.LogTrace("DB clear");

    logger.LogTrace("Starting DB-Transaction");
    using var tx = db.Database.BeginTransaction();

    try
    {
        logger.LogInformation("Starting Integration-Import");
        foreach (var integration in config.Integrations)
        {
            var balances = await cryptoTrackerLogic.GetAvailableIntegrationBalances(integration);

            logger.LogTrace($"Fetched {balances.Count()} balances for {integration.Name}");

            foreach (var balance in balances)
            {
                AddMeasuring(db, integration, balance.Symbol, balance.Balance);
            }

        }
        logger.LogInformation("Finished Integration-Import");

        logger.LogInformation("Starting Metadataimport");
        await UpdateAssetMetadata(db);
        logger.LogInformation("Finished Metadataimport");

        tx.Commit();

        logger.LogInformation("Finished Import");
    }
    catch (Exception ex)
    {
        logger.LogError(ex.ToString());
        logger.LogTrace("Rolling back transaction");
        tx.Rollback();
    }
}


void AddMeasuring(DatabaseContext db, CryptotrackerIntegration integration, string symbol, decimal balance)
{
    var ex = db.ExchangeIntegrations.FirstOrDefault(x => x.Name.ToLower() == integration.Name.ToLower());

    if (ex == null)
    {
        ex = new ExchangeIntegration()
        {
            Name = integration.Name,
            Description = integration.Description
        };
        logger.LogTrace($"Adding new ExchangeIntegration: {ex.Name}");
        db.ExchangeIntegrations.Add(ex);
    }

    var asset = db.Assets.Find(symbol);

    if (asset == null)
    {
        asset = new Asset()
        {
            Symbol = symbol,
            IsFiat = false,
            IsHidden = false
        };
        logger.LogTrace($"Adding new Asset: {asset.Symbol}");
        db.Assets.Add(asset);
    }

    var measuring = new AssetMeasuring()
    {
        Symbol = asset.Symbol,
        IntegrationId = ex.Id,
        Timestamp = DateTime.Now,
        Amount = balance
    };

    db.AssetMeasurings.Add(measuring);
    logger.LogTrace($"Adding new AssetMeasuring to {ex.Name} for {measuring.Symbol} - {measuring.Amount}");
    db.SaveChanges();
}

async Task UpdateAssetMetadata(DatabaseContext db)
{
    var assets = db.Assets.ToList();
    logger.LogTrace($"Found {assets.Count} assets");

    if (assets.Count == 0) return;

    var coinList = cryptoTrackerLogic.GetCoinList().Result;
    logger.LogTrace($"Fetched {coinList.Count()} coins");

    if (coinList != null)
    {
        foreach (var asset in assets.Where(x => !x.IsFiat))
        {
            Coin? coin = null;
            if (string.IsNullOrWhiteSpace(asset.ExternalId))
            {
                var coins = coinList.Where(x => x.Symbol.ToLower() == asset.Symbol.ToLower());

                if (coins.Count() != 1) continue;

                coin = coins.First();
            }
            else
            {
                coin = coinList.FirstOrDefault(x => x.Id.ToLower() == asset.ExternalId.ToLower());
            }

            if (coin == null) continue;

            if (string.IsNullOrWhiteSpace(asset.Name))
            {
                logger.LogTrace($"Update name for '{asset.Symbol}' to '{coin.Value.Name}'");
                asset.Name = coin.Value.Name;
            }

            if (string.IsNullOrWhiteSpace(asset.ExternalId))
            {
                logger.LogTrace($"Update externalId for '{asset.Symbol}' to '{coin.Value.Id}'");
                asset.ExternalId = coin.Value.Id;
            }
        }
        db.SaveChanges();
    }

    var fiatList = cryptoTrackerLogic.GetFiatList().Result;
    logger.LogTrace($"Fetched {fiatList.Count()} fiats");

    if (fiatList != null)
    {
        foreach (var asset in assets.Where(x => x.IsFiat))
        {
            Fiat? fiat = null;
            if (string.IsNullOrWhiteSpace(asset.ExternalId))
            {
                var fiats = fiatList.Where(x => x.Symbol.ToLower() == asset.Symbol.ToLower());

                if (fiats.Count() != 1) continue;

                fiat = fiats.First();
            }
            else
            {
                fiat = fiatList.FirstOrDefault(x => x.Symbol.ToLower() == asset.ExternalId.ToLower());
            }

            if (fiat == null) continue;

            if (string.IsNullOrWhiteSpace(asset.Name))
            {
                logger.LogTrace($"Update name for '{asset.Symbol}' to '{fiat.Value.Name}'");
                asset.Name = fiat.Value.Name;
            }
            if (string.IsNullOrWhiteSpace(asset.ExternalId))
            {

                logger.LogTrace($"Update externalId for '{asset.Symbol}' to '{fiat.Value.Symbol}'");
                asset.ExternalId = fiat.Value.Symbol;
            }
        }
        db.SaveChanges();
    }

    var foundExternalIds = db.Assets.Where(x => !string.IsNullOrWhiteSpace(x.ExternalId)).Select(x => new { x.ExternalId, x.IsFiat }).ToList();

    if (foundExternalIds.Count == 0) return;
    var currency = "chf";
    var coinDataList = await cryptoTrackerLogic.GetCoinData(currency, foundExternalIds.Where(x => !x.IsFiat).Select(x => x.ExternalId!).ToList());
    var fiatDataList = await cryptoTrackerLogic.GetFiatData(currency, foundExternalIds.Where(x => x.IsFiat).Select(x => x.ExternalId!).ToList());

    var all = coinDataList.Union(fiatDataList).ToList();

    all.ForEach(item =>
    {
        var asset = db.Assets.FirstOrDefault(a => a.ExternalId == item.AssetId);

        if (asset == null) return;

        if (string.IsNullOrWhiteSpace(asset.Image))
            asset.Image = item.Image;

        var price = db.AssetPriceHistory.FirstOrDefault(p => p.Symbol == asset.Symbol && p.Date == DateTime.Today && p.Currency == currency);

        if (price == null)
        {
            price = new AssetPriceHistory()
            {
                Symbol = asset.Symbol,
                Date = DateTime.Today,
                Currency = currency,
                Price = item.Price,
            };

            logger.LogTrace($"Add AssetPriceHistory for {price.Symbol}, {price.Date} - {price.Price} {price.Currency}");

            db.AssetPriceHistory.Add(price);
        }
        else
        {
            logger.LogTrace($"Update AssetPriceHistory for {price.Symbol}, {price.Date} from {price.Price} {price.Currency} to {item.Price} {price.Currency}");
            price.Price = item.Price;
        }
    });

    db.SaveChanges();
}