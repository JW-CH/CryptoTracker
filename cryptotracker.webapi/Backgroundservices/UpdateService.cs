using System.Threading.Tasks;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

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
                    var cryptoTrackerLogic = scope.ServiceProvider.GetRequiredService<ICryptoTrackerLogic>();
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

    async Task Import(DatabaseContext db, ICryptoTrackerLogic cryptoTrackerLogic, CryptoTrackerAssetLogic cryptoTrackerAssetLogic)
    {
        _logger.LogTrace("Starting DB-Transaction");
        using var tx = await db.Database.BeginTransactionAsync();

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

                await db.SaveChangesAsync();
                _logger.LogTrace("DB clear");

                var balances = await cryptoTrackerLogic.GetAvailableIntegrationBalances(integration);

                _logger.LogTrace($"Fetched {balances.Count()} balances for {integration.Name}");

                foreach (var balance in balances)
                {
                    await AddMeasuring(db, integration, balance.Symbol, balance.Balance);
                }
            }
            _logger.LogInformation("Finished Integration-Import");

            _logger.LogInformation("Starting Metadataimport");
            await cryptoTrackerAssetLogic.UpdateAllAssetMetadata(db);
            _logger.LogInformation("Finished Metadataimport");

            await tx.CommitAsync();

            _logger.LogInformation("Finished Import");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            _logger.LogTrace("Rolling back transaction");
            await tx.RollbackAsync();
        }
    }

    async Task AddMeasuring(DatabaseContext db, CryptoTrackerIntegration integration, string symbol, decimal balance)
    {
        var ex = await db.ExchangeIntegrations.FirstOrDefaultAsync(x => x.Name.ToLower() == integration.Name.ToLower());

        if (ex == null)
        {
            ex = new ExchangeIntegration()
            {
                Name = integration.Name,
                Description = integration.Description
            };
            _logger.LogTrace($"Adding new ExchangeIntegration: {ex.Name}");
            await db.ExchangeIntegrations.AddAsync(ex);
        }

        var asset = await db.Assets.FindAsync(symbol);

        if (asset == null)
        {
            asset = new Asset()
            {
                Symbol = symbol,
                AssetType = AssetType.Crypto,
                IsHidden = false
            };
            _logger.LogTrace($"Adding new Asset: {asset.Symbol}");
            await db.Assets.AddAsync(asset);
        }

        var measuring = new AssetMeasuring()
        {
            Symbol = asset.Symbol,
            IntegrationId = ex.Id,
            Timestamp = DateTime.UtcNow,
            Amount = balance
        };

        await db.AssetMeasurings.AddAsync(measuring);
        _logger.LogTrace($"Adding new AssetMeasuring to {ex.Name} for {measuring.Symbol} - {measuring.Amount}");
        await db.SaveChangesAsync();
    }
}