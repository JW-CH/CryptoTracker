using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;

public class UpdateService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UpdateService> _logger;
    private readonly ICryptoTrackerConfig _config;
    private readonly TimeSpan _delay;

    public UpdateService(IServiceScopeFactory scopeFactory, ILogger<UpdateService> logger, ICryptoTrackerConfig config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
        _delay = TimeSpan.FromMinutes(_config.Interval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var timer = new PeriodicTimer(_delay))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    _logger.LogInformation("Starting import");

                    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                    var cryptoTrackerLogic = scope.ServiceProvider.GetRequiredService<CryptoTrackerLogic>();
                    var fiatLogic = scope.ServiceProvider.GetRequiredService<IFiatLogic>();
                    var stockLogic = scope.ServiceProvider.GetRequiredService<IStockLogic>();
                    var ctal = new CryptoTrackerAssetLogic(_logger, cryptoTrackerLogic, fiatLogic, stockLogic);

                    await Import(db, cryptoTrackerLogic, ctal);
                    _logger.LogInformation("Import finished");

                    _logger.LogInformation($"Waiting {_config.Interval} minutes");

                }
                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
    }

    async Task Import(DatabaseContext db, CryptoTrackerLogic cryptoTrackerLogic, CryptoTrackerAssetLogic cryptoTrackerAssetLogic)
    {
        _logger.LogTrace("Starting DB-Transaction");
        using var tx = db.Database.BeginTransaction();

        try
        {
            _logger.LogInformation("Starting Integration-Import");

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            foreach (var integration in _config.Integrations)
            {
                _logger.LogTrace($"Clearing today's AssetMeasurings entries for integration {integration.Name}");
                var entries = db.AssetMeasurings.Where(x => x.Timestamp >= today && x.Timestamp < tomorrow && x.Integration.Name == integration.Name);
                var count = entries.Count();
                db.AssetMeasurings.RemoveRange(entries);
                _logger.LogTrace($"Removed {count} AssetMeasurings for integration {integration.Name}");

                db.SaveChanges();
                _logger.LogTrace("DB clear");

                var balances = await cryptoTrackerLogic.GetAvailableIntegrationBalances(integration);

                _logger.LogTrace($"Fetched {balances.Count()} balances for {integration.Name}");

                foreach (var balance in balances)
                {
                    AddMeasuring(db, integration, balance.Symbol, balance.Balance);
                }
            }
            _logger.LogInformation("Finished Integration-Import");

            _logger.LogInformation("Starting Metadataimport");
            await cryptoTrackerAssetLogic.UpdateAllAssetMetadata(db);
            _logger.LogInformation("Finished Metadataimport");

            tx.Commit();

            _logger.LogInformation("Finished Import");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            _logger.LogTrace("Rolling back transaction");
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
            _logger.LogTrace($"Adding new ExchangeIntegration: {ex.Name}");
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
            _logger.LogTrace($"Adding new Asset: {asset.Symbol}");
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
        _logger.LogTrace($"Adding new AssetMeasuring to {ex.Name} for {measuring.Symbol} - {measuring.Amount}");
        db.SaveChanges();
    }
}