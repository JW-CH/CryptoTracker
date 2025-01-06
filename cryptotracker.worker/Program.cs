using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text;

// Create a LoggerFactory
using var loggerFactory = LoggerFactory.Create(builder =>
{
    _ = builder.AddSimpleConsole(options =>
                {
                    // Customizing the log output format
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";  // Custom timestamp format
                })
            .SetMinimumLevel(LogLevel.Information); // Set minimum log level
});

// Create a logger instance
var logger = loggerFactory.CreateLogger("Cryptotracker");

var root = Directory.GetCurrentDirectory();
string ymlConfigPath;
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

    logger.LogInformation("Config loaded");

    logger.LogInformation($"Connectionstring: {config.ConnectionString}");
    logger.LogInformation($"Integrations: {config.Integrations.Count}");
}
else
{
    throw new Exception("Config file not found");
}

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

    logger.LogTrace("Clearing today's DB entries");

    db.AssetMeasurings.RemoveRange(db.AssetMeasurings.Where(x => x.StandingDate.Date == DateTime.Now.Date));
    //db.Assets.RemoveRange(db.Assets);
    //db.ExchangeIntegrations.RemoveRange(db.ExchangeIntegrations);

    db.SaveChanges();

    logger.LogTrace("DB clear");

    using var tx = db.Database.BeginTransaction();

    StringBuilder sb = new StringBuilder();

    try
    {
        foreach (var integration in config.Integrations)
        {
            var balances = await CryptoTrackerLogic.GetAvailableIntegrationBalances(integration);

            foreach (var balance in balances)
            {
                AddMeasuring(db, integration, balance.Symbol, balance.Balance);

                sb.AppendLine($"{integration.Name} - {balance.Symbol}: {balance.Balance}");
            }

        }
        logger.LogInformation(sb.ToString());

        logger.LogTrace("Starting Metadataimport");
        UpdateAssetMetadata(db);
        logger.LogTrace("Finished Metadataimport");

        tx.Commit();

        logger.LogInformation("Finished Import");
    }
    catch (Exception ex)
    {
        logger.LogError(ex.ToString());
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
        db.ExchangeIntegrations.Add(ex);
    }

    var asset = db.Assets.Find(symbol);

    if (asset == null)
    {
        asset = new Asset()
        {
            Symbol = symbol,
            Name = "",
            ExternalId = ""
        };
        db.Assets.Add(asset);
    }

    var x = new AssetMeasuring()
    {
        Asset = asset,
        Integration = ex,
        StandingDate = DateTime.Now,
        StandingValue = balance
    };

    db.AssetMeasurings.Add(x);
    db.SaveChanges();
}

void UpdateAssetMetadata(DatabaseContext db)
{
    var assets = db.Assets.ToList();

    if (assets.Count == 0) return;

    var coinList = CryptoTrackerLogic.GetCoinList().Result;

    if (coinList == null) return;

    foreach (var asset in assets)
    {
        Coin? coin = null;
        if (string.IsNullOrWhiteSpace(asset.ExternalId))
        {
            var coins = coinList.Where(x => x.symbol.ToLower() == asset.Symbol.ToLower());

            if (coins.Count() != 1) continue;

            coin = coins.First();
        }
        else
        {
            coin = coinList.FirstOrDefault(x => x.id == asset.ExternalId);
        }

        if (coin == null) continue;

        asset.Name = coin.Value.name;
        if (string.IsNullOrWhiteSpace(asset.ExternalId))
            asset.ExternalId = coin.Value.id;
    }
    db.SaveChanges();

    var foundExternalIds = db.Assets.Where(x => !string.IsNullOrWhiteSpace(x.ExternalId)).Select(x => x.ExternalId).ToList();

    if (foundExternalIds.Count == 0) return;
    var currency = "chf";
    var coinDataList = CryptoTrackerLogic.GetCoinData(currency, foundExternalIds).Result;

    if (coinDataList == null) return;

    coinDataList.ForEach(coin =>
    {
        var asset = db.Assets.FirstOrDefault(a => a.ExternalId == coin.AssetId);

        if (asset == null) return;

        if (string.IsNullOrWhiteSpace(asset.Image))
            asset.Image = coin.Image;

        var price = db.AssetPriceHistory.FirstOrDefault(p => p.Symbol == asset.Symbol && p.Date == DateTime.Today && p.Currency == currency);

        if (price == null)
        {
            price = new AssetPriceHistory()
            {
                Symbol = asset.Symbol,
                Date = DateTime.Today,
                Currency = currency,
                Price = coin.Price,
            };

            db.AssetPriceHistory.Add(price);
        }
        else
        {
            price.Price = coin.Price;
        }
    });

    db.SaveChanges();
}