using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Text;

var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
optionsBuilder.UseMySQL("server=192.168.0.165;database=cryptotracker;user=root;password=strong_password;");

using var db = new DatabaseContext(optionsBuilder.Options);

Console.WriteLine("Clearing today's DB entries");

db.AssetMeasurings.RemoveRange(db.AssetMeasurings.Where(x => x.StandingDate.Date == DateTime.Now.Date));
//db.Assets.RemoveRange(db.Assets);
//db.ExchangeIntegrations.RemoveRange(db.ExchangeIntegrations);

db.SaveChanges();

Console.WriteLine("DB clear");

using var tx = db.Database.BeginTransaction();

StringBuilder sb = new StringBuilder();

var root = Directory.GetCurrentDirectory();
string ymlConfigPath;
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    ymlConfigPath = Path.Combine(root, "docker", "config.yml");
}
else
{
    ymlConfigPath = Path.Combine(root, "..", "..", "..", "..", "docker", "config.yml");
}

CryptotrackerConfig config;

if (File.Exists(ymlConfigPath))
{
    var yml = File.ReadAllText(ymlConfigPath);

    config = CryptotrackerConfig.LoadFromYml(yml);
}
else
{
    config = new CryptotrackerConfig();
}
try
{
    foreach (var integration in config.Integrations)
    {
        var balances = await CryptoTrackerLogic.GetAvailableIntegrationBalances(integration);

        foreach (var balance in balances)
        {
            AddMeasuring(integration, balance.Symbol, balance.Balance);

            sb.AppendLine($"{integration.Name} - {balance.Symbol}: {balance.Balance}");
        }

    }
    Console.WriteLine(sb.ToString());

    Console.WriteLine("Starting Metadataimport");
    UpdateAssetMetadata();
    Console.WriteLine("Finished Metadataimport");

    tx.Commit();

    Console.WriteLine("Finished Import");
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
    tx.Rollback();
}


void AddMeasuring(CryptotrackerIntegration integration, string symbol, decimal balance)
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

void UpdateAssetMetadata()
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
    var priceList = CryptoTrackerLogic.GetCoinPrices(currency, foundExternalIds).Result;

    if (priceList == null) return;

    priceList.ForEach(x =>
    {
        var asset = db.Assets.FirstOrDefault(a => a.ExternalId == x.AssetId);

        if (asset == null) return;

        var price = db.AssetPriceHistory.FirstOrDefault(p => p.Symbol == asset.Symbol && p.Date == DateTime.Today && p.Currency == currency);

        if (price == null)
        {
            price = new AssetPriceHistory()
            {
                Symbol = asset.Symbol,
                Date = DateTime.Today,
                Currency = currency,
                Price = x.Price,
            };

            db.AssetPriceHistory.Add(price);
        }
        else
        {
            price.Price = x.Price;
        }
    });

    db.SaveChanges();
}