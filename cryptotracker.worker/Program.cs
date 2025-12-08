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

CryptoTrackerConfig config;

if (File.Exists(ymlConfigPath))
{
    var yml = File.ReadAllText(ymlConfigPath);

    config = CryptoTrackerConfig.LoadFromYml(yml);
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
var fiatLogic = new FiatLogic(logger);
var stockLogic = new YahooFinanceStockLogic(logger, fiatLogic);
var cryptoTrackerAssetLogic = new CryptoTrackerAssetLogic(logger, cryptoTrackerLogic, fiatLogic, stockLogic);

var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
optionsBuilder.UseNpgsql(config.ConnectionString);

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

    logger.LogTrace("Starting DB-Transaction");
    using var tx = db.Database.BeginTransaction();

    try
    {
        logger.LogInformation("Starting Integration-Import");

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        foreach (var integration in config.Integrations)
        {
            logger.LogTrace($"Clearing today's AssetMeasurings entries for integration {integration.Name}");
            var entries = db.AssetMeasurings.Where(x => x.Timestamp >= today && x.Timestamp < tomorrow && x.Integration.Name == integration.Name);
            var count = entries.Count();
            db.AssetMeasurings.RemoveRange(entries);
            logger.LogTrace($"Removed {count} AssetMeasurings for integration {integration.Name}");

            db.SaveChanges();
            logger.LogTrace("DB clear");

            var balances = await cryptoTrackerLogic.GetAvailableIntegrationBalances(integration);

            logger.LogTrace($"Fetched {balances.Count()} balances for {integration.Name}");

            foreach (var balance in balances)
            {
                AddMeasuring(db, integration, balance.Symbol, balance.Balance);
            }
        }
        logger.LogInformation("Finished Integration-Import");

        logger.LogInformation("Starting Metadataimport");
        await cryptoTrackerAssetLogic.UpdateAllAssetMetadata(db);
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


void AddMeasuring(DatabaseContext db, CryptoTrackerIntegration integration, string symbol, decimal balance)
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
            AssetType = AssetType.Crypto,
            IsHidden = false
        };
        logger.LogTrace($"Adding new Asset: {asset.Symbol}");
        db.Assets.Add(asset);
    }

    var measuring = new AssetMeasuring()
    {
        Symbol = asset.Symbol,
        IntegrationId = ex.Id,
        Timestamp = DateTime.UtcNow,
        Amount = balance
    };

    db.AssetMeasurings.Add(measuring);
    logger.LogTrace($"Adding new AssetMeasuring to {ex.Name} for {measuring.Symbol} - {measuring.Amount}");
    db.SaveChanges();
}