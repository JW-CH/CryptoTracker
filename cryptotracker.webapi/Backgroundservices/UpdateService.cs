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
                    var currencyProvider = scope.ServiceProvider.GetRequiredService<ICurrencyProvider>();
                    var stockLogic = scope.ServiceProvider.GetRequiredService<IStockLogic>();
                    var ctal = new CryptoTrackerAssetLogic(_logger, cryptoTrackerLogic, currencyProvider, stockLogic);

                    try
                    {
                        await Import(db, cryptoTrackerLogic, ctal);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Import run failed");
                    }
                    _logger.LogInformation("Import finished");

                    _logger.LogInformation($"Waiting {_config.Interval} minutes");

                }
                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
    }

    async Task Import(DatabaseContext db, ICryptoTrackerLogic cryptoTrackerLogic, CryptoTrackerAssetLogic cryptoTrackerAssetLogic)
    {
        _logger.LogInformation("Starting Integration-Import");

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        foreach (var integration in _config.Integrations)
        {
            _logger.LogTrace("Starting DB-Transaction");
            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var balances = await cryptoTrackerLogic.GetAvailableIntegrationBalances(integration);
                _logger.LogTrace($"Fetched {balances.Count()} balances for {integration.Name}");

                _logger.LogTrace($"Clearing today's AssetMeasurings entries for integration {integration.Name}");
                var entries = db.AssetMeasurings.Where(x => x.Timestamp >= today && x.Timestamp < tomorrow && x.Integration.Name == integration.Name);
                var count = entries.Count();
                db.AssetMeasurings.RemoveRange(entries);
                _logger.LogTrace($"Removed {count} AssetMeasurings for integration {integration.Name}");

                foreach (var balance in balances)
                {
                    await AddMeasuring(db, integration, balance.Symbol, balance.Balance);
                }
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing integration {Name}, skipping", integration.Name);
                _logger.LogTrace("Rolling back transaction");
                await tx.RollbackAsync();
                db.ChangeTracker.Clear();
            }
        }
        _logger.LogInformation("Finished Integration-Import");

        _logger.LogInformation("Starting Metadataimport");
        try
        {
            await cryptoTrackerAssetLogic.UpdateAllAssetMetadata(db);
            _logger.LogInformation("Finished Metadataimport");
        }
        catch (Exception ex)
        {
            // an unhandled exception here would stop the whole host (BackgroundService
            // default is StopHost); balances are already committed at this point
            _logger.LogError(ex, "Metadata import failed, keeping already imported balances");
            db.ChangeTracker.Clear();
        }

        _logger.LogInformation("Finished Import");
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
            await db.SaveChangesAsync();
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
    }
}