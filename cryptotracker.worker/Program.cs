using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;
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

var ymlConfigPath = Path.Combine(root, "docker", "config.yml");
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
    tx.Commit();
    Console.WriteLine(sb.ToString());
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
            AssetId = symbol,
            Name = "",
            Description = ""
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