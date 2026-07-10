using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Backgroundservices;

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
                    var integrationProviders = scope.ServiceProvider.GetRequiredService<IEnumerable<IIntegrationProvider>>();
                    var priceProviders = scope.ServiceProvider.GetRequiredService<IEnumerable<IPriceProvider>>();
                    var assetMetadataService = scope.ServiceProvider.GetRequiredService<AssetMetadataService>();

                    try
                    {
                        var currencyProvider = priceProviders.First(p => p.Handles.Contains(AssetType.Fiat));
                        await Import(db, integrationProviders, currencyProvider, assetMetadataService);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Import run failed");
                    }
                    _logger.LogInformation("Import finished");

                    _logger.LogInformation("Waiting {Interval} minutes", _config.Interval);

                }
                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
    }

    internal async Task Import(DatabaseContext db, IEnumerable<IIntegrationProvider> integrationProviders, IPriceProvider currencyProvider, AssetMetadataService assetMetadataService)
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
                var provider = integrationProviders.FirstOrDefault(p => p.Type == integration.Type)
                    ?? throw new InvalidOperationException($"No integration provider found for type {integration.Type}");

                var balances = await provider.GetBalancesAsync(integration);
                _logger.LogTrace("Fetched {Count} balances for {Name}", balances.Count(), integration.Name);

                var exchangeIntegration = await GetOrCreateExchangeIntegration(db, integration);

                // symbols the last snapshot still had but the exchange no longer reports:
                // their balance dropped to 0 (exchanges omit empty positions)
                var zeroSymbols = await GetDisappearedSymbols(db, exchangeIntegration.Id, balances, today);

                _logger.LogTrace("Clearing today's AssetMeasurings entries for integration {Name}", integration.Name);
                var entries = db.AssetMeasurings.Where(x => x.Timestamp >= today && x.Timestamp < tomorrow && x.IntegrationId == exchangeIntegration.Id);
                var count = entries.Count();
                db.AssetMeasurings.RemoveRange(entries);
                _logger.LogTrace("Removed {Count} AssetMeasurings for integration {Name}", count, integration.Name);

                foreach (var balance in balances)
                {
                    await AddMeasuring(db, currencyProvider, exchangeIntegration, balance.Symbol, balance.Balance, balance.AssetType);
                }
                foreach (var symbol in zeroSymbols)
                {
                    _logger.LogInformation("Asset {Symbol} no longer reported by {Name}, recording balance 0", symbol, integration.Name);
                    await AddMeasuring(db, currencyProvider, exchangeIntegration, symbol, 0m, null);
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
            await assetMetadataService.UpdateAllAssetMetadataAsync();
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

    async Task<ExchangeIntegration> GetOrCreateExchangeIntegration(DatabaseContext db, CryptoTrackerIntegration integration)
    {
        var ex = await db.ExchangeIntegrations.FirstOrDefaultAsync(x => x.Name.ToLower() == integration.Name.ToLower());

        if (ex == null)
        {
            ex = new ExchangeIntegration()
            {
                Name = integration.Name,
                Description = integration.Description
            };
            _logger.LogTrace("Adding new ExchangeIntegration: {Name}", ex.Name);
            await db.ExchangeIntegrations.AddAsync(ex);
            await db.SaveChangesAsync();
        }

        return ex;
    }

    /// <summary>
    /// Returns the symbols that had a non-zero balance in the integration's most recent
    /// snapshot before <paramref name="today"/> but are missing from the freshly fetched
    /// balances — i.e. positions that were emptied since the last import.
    /// </summary>
    async Task<List<string>> GetDisappearedSymbols(DatabaseContext db, Guid integrationId, IEnumerable<BalanceResult> balances, DateTime today)
    {
        var lastTimestamp = await db.AssetMeasurings
            .Where(m => m.IntegrationId == integrationId && m.Timestamp < today)
            .MaxAsync(m => (DateTime?)m.Timestamp);

        if (lastTimestamp == null) return new();

        var lastDay = lastTimestamp.Value.Date;
        var lastDayEnd = lastDay.AddDays(1);

        // Amount != 0 keeps the zero-markers self-terminating: an asset recorded as 0
        // is no longer part of the previous snapshot and won't get another 0 tomorrow
        var previousSymbols = await db.AssetMeasurings
            .Where(m => m.IntegrationId == integrationId
                     && m.Timestamp >= lastDay && m.Timestamp < lastDayEnd
                     && m.Amount != 0)
            .Select(m => m.Symbol)
            .Distinct()
            .ToListAsync();

        var currentSymbols = balances.Select(b => b.Symbol).ToHashSet();

        return previousSymbols.Where(s => !currentSymbols.Contains(s)).ToList();
    }

    async Task AddMeasuring(DatabaseContext db, IPriceProvider currencyProvider, ExchangeIntegration exchangeIntegration, string symbol, decimal balance, AssetType? assetTypeHint)
    {
        var asset = await db.Assets.FindAsync(symbol);

        if (asset == null)
        {
            asset = new Asset()
            {
                Symbol = symbol,
                AssetType = assetTypeHint ?? await ResolveAssetTypeAsync(currencyProvider, symbol),
                IsHidden = false
            };
            _logger.LogTrace("Adding new Asset: {Symbol} ({AssetType})", asset.Symbol, asset.AssetType);
            await db.Assets.AddAsync(asset);
        }

        var measuring = new AssetMeasuring()
        {
            Symbol = asset.Symbol,
            IntegrationId = exchangeIntegration.Id,
            Timestamp = DateTime.UtcNow,
            Amount = balance
        };

        await db.AssetMeasurings.AddAsync(measuring);
        _logger.LogTrace("Adding new AssetMeasuring to {Name} for {Symbol} - {Amount}", exchangeIntegration.Name, measuring.Symbol, measuring.Amount);
    }

    /// <summary>
    /// Fallback for sources that don't report an asset type: symbols matching a known
    /// fiat currency are treated as fiat, everything else as crypto.
    /// </summary>
    async Task<AssetType> ResolveAssetTypeAsync(IPriceProvider currencyProvider, string symbol)
    {
        try
        {
            var currencies = await currencyProvider.GetAssetsAsync();
            if (currencies.Any(c => c.Symbol.ToLower() == symbol.ToLower())) return AssetType.Fiat;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch currency list to classify {Symbol}, defaulting to Crypto", symbol);
        }
        return AssetType.Crypto;
    }
}