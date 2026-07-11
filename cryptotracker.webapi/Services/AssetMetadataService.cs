using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Services
{
    public class AssetMetadataService
    {
        private readonly ILogger<AssetMetadataService> _logger;
        private readonly DatabaseContext _db;
        private readonly IEnumerable<IPriceProvider> _priceProviders;
        private readonly ICryptoTrackerConfig _config;
        private readonly PortfolioClock _clock;

        public AssetMetadataService(ILogger<AssetMetadataService> logger, DatabaseContext db, IEnumerable<IPriceProvider> priceProviders, ICryptoTrackerConfig config, PortfolioClock clock)
        {
            _logger = logger;
            _db = db;
            _priceProviders = priceProviders;
            _config = config;
            _clock = clock;
        }

        /// <summary>
        /// Liefert null, wenn der Provider nichts Brauchbares (keine AssetId) zurückgibt.
        /// </summary>
        public async Task<AssetMetadata?> FetchMetadataAsync(AssetType assetType, string externalId)
        {
            var currency = _config.BaseCurrency;

            AssetMetadata metadata;

            var priceProvider = GetPriceProviderOrThrow(assetType);

            metadata = (await priceProvider.GetQuotesAsync(currency, new List<string> { externalId })).FirstOrDefault();

            return string.IsNullOrEmpty(metadata.AssetId) ? null : metadata;
        }

        public async Task UpdateMetadataForAssetAsync(AssetMetadata metadata)
        {
            await ApplyMetadataAsync(metadata);
            await _db.SaveChangesAsync();
        }

        private async Task ApplyMetadataAsync(AssetMetadata metadata)
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a => a.ExternalId == metadata.AssetId);

            if (asset == null) return;

            if (string.IsNullOrWhiteSpace(asset.Name))
                asset.Name = metadata.Name;

            if (string.IsNullOrWhiteSpace(asset.Image))
                asset.Image = metadata.Image;

            var currency = _config.BaseCurrency;
            var today = _clock.Today;
            var price = await _db.AssetPriceHistory.FirstOrDefaultAsync(p => p.Symbol == asset.Symbol && p.Currency == currency && p.Date == today);

            if (price == null)
            {
                price = new AssetPriceHistory()
                {
                    Symbol = asset.Symbol,
                    Date = today,
                    Currency = currency,
                    Price = metadata.Price,
                };

                _logger.LogTrace("Add AssetPriceHistory for {Symbol}, {Date} - {Price} {Currency}", price.Symbol, price.Date, price.Price, price.Currency);

                await _db.AssetPriceHistory.AddAsync(price);
            }
            else
            {
                _logger.LogTrace("Update AssetPriceHistory for {Symbol}, {Date} from {OldPrice} to {NewPrice} {Currency}", price.Symbol, price.Date, price.Price, metadata.Price, price.Currency);
                price.Price = metadata.Price;
            }
        }

        private IPriceProvider GetPriceProviderOrThrow(AssetType assetType)
        {
            var provider = _priceProviders.FirstOrDefault(x => x.Handles.Contains(assetType)) ?? throw new InvalidOperationException($"No price provider found for asset type {assetType}");

            return provider;
        }

        private IPriceProvider? GetPriceProvider(AssetType assetType)
        {
            var provider = _priceProviders.FirstOrDefault(x => x.Handles.Contains(assetType));

            if (provider == null)
                _logger.LogWarning("Assets of type {AssetType} exist but no price provider is configured for it, skipping", assetType);

            return provider;
        }

        public async Task UpdateAllAssetMetadataAsync()
        {
            var assets = await _db.Assets.ToListAsync();
            var currency = _config.BaseCurrency;
            _logger.LogTrace("Found {Count} assets", assets.Count);

            if (assets.Count == 0) return;

            var cryptoAssets = assets.Where(x => x.AssetType == AssetType.Crypto).ToList();

            var cryptoPriceProvider = cryptoAssets.Any() ? GetPriceProvider(AssetType.Crypto) : null;
            if (cryptoPriceProvider != null)
            {
                var coinList = await cryptoPriceProvider.GetAssetsAsync();
                _logger.LogTrace("Fetched {Count} coins", coinList.Count());
                foreach (var asset in cryptoAssets)
                {
                    ProviderAsset? coin = null;
                    if (string.IsNullOrWhiteSpace(asset.ExternalId))
                    {
                        var coins = coinList.Where(x => x.Symbol.ToLower() == asset.Symbol.ToLower());

                        if (coins.Count() != 1) continue;

                        coin = coins.First();
                    }
                    else
                    {
                        coin = coinList.FirstOrDefault(x => x.ExternalId.ToLower() == asset.ExternalId.ToLower());
                    }

                    if (coin == null) continue;

                    if (string.IsNullOrWhiteSpace(asset.Name))
                    {
                        _logger.LogTrace("Update name for '{Symbol}' to '{Name}'", asset.Symbol, coin.Value.Name);
                        asset.Name = coin.Value.Name;
                    }

                    if (string.IsNullOrWhiteSpace(asset.ExternalId))
                    {
                        _logger.LogTrace("Update externalId for '{Symbol}' to '{ExternalId}'", asset.Symbol, coin.Value.ExternalId);
                        asset.ExternalId = coin.Value.ExternalId;
                    }
                }
                await _db.SaveChangesAsync();
            }


            var fiatAssets = assets.Where(x => x.AssetType == AssetType.Fiat).ToList();

            var currencyPriceProvider = fiatAssets.Any() ? GetPriceProvider(AssetType.Fiat) : null;
            if (currencyPriceProvider != null)
            {
                var currencyList = await currencyPriceProvider.GetAssetsAsync();
                _logger.LogTrace("Fetched {Count} currencies", currencyList.Count());
                foreach (var asset in fiatAssets)
                {
                    ProviderAsset? matchedCurrency = null;
                    if (string.IsNullOrWhiteSpace(asset.ExternalId))
                    {
                        var matches = currencyList.Where(x => x.Symbol.ToLower() == asset.Symbol.ToLower());

                        if (matches.Count() != 1) continue;

                        matchedCurrency = matches.First();
                    }
                    else
                    {
                        matchedCurrency = currencyList.FirstOrDefault(x => x.Symbol.ToLower() == asset.ExternalId.ToLower());
                    }

                    if (matchedCurrency == null) continue;

                    if (string.IsNullOrWhiteSpace(asset.Name))
                    {
                        _logger.LogTrace("Update name for '{Symbol}' to '{Name}'", asset.Symbol, matchedCurrency.Value.Name);
                        asset.Name = matchedCurrency.Value.Name;
                    }
                    if (string.IsNullOrWhiteSpace(asset.ExternalId))
                    {
                        _logger.LogTrace("Update externalId for '{Symbol}' to '{ExternalId}'", asset.Symbol, matchedCurrency.Value.Symbol);
                        asset.ExternalId = matchedCurrency.Value.Symbol;
                    }
                }
                await _db.SaveChangesAsync();
            }

            var foundExternalIds = await _db.Assets.Where(x => !string.IsNullOrWhiteSpace(x.ExternalId)).Select(x => new { x.ExternalId, x.AssetType }).ToListAsync();

            if (foundExternalIds.Count == 0) return;

            var coinDataList = cryptoPriceProvider != null ? await cryptoPriceProvider.GetQuotesAsync(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Crypto).Select(x => x.ExternalId!).ToList()) : new List<AssetMetadata>();
            var currencyDataList = currencyPriceProvider != null ? await currencyPriceProvider.GetQuotesAsync(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Fiat).Select(x => x.ExternalId!).ToList()) : new List<AssetMetadata>();

            var stockPriceProvider = foundExternalIds.Any(x => x.AssetType == AssetType.Stock) ? GetPriceProvider(AssetType.Stock) : null;
            var stockDataList = stockPriceProvider != null ? await stockPriceProvider.GetQuotesAsync(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Stock).Select(x => x.ExternalId!).ToList()) : new List<AssetMetadata>();

            var all = coinDataList.Union(currencyDataList).Union(stockDataList).ToList();

            foreach (var item in all)
            {
                await ApplyMetadataAsync(item);
            }

            await _db.SaveChangesAsync();
        }
    }
}
