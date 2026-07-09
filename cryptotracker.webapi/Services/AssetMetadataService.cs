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
        private readonly ICryptoTrackerLogic _cryptoTrackerLogic;
        private readonly ICurrencyProvider _currencyProvider;
        private readonly IStockLogic _stockLogic;
        private readonly ICryptoTrackerConfig _config;

        public AssetMetadataService(ILogger<AssetMetadataService> logger, DatabaseContext db, ICryptoTrackerLogic cryptoTrackerLogic, ICurrencyProvider currencyProvider, IStockLogic stockLogic, ICryptoTrackerConfig config)
        {
            _logger = logger;
            _db = db;
            _cryptoTrackerLogic = cryptoTrackerLogic;
            _currencyProvider = currencyProvider;
            _stockLogic = stockLogic;
            _config = config;
        }

        /// <summary>
        /// Liefert null, wenn der Provider nichts Brauchbares (keine AssetId) zurückgibt.
        /// </summary>
        public async Task<AssetMetadata?> FetchMetadataAsync(AssetType assetType, string externalId)
        {
            var currency = _config.BaseCurrency;

            AssetMetadata metadata;
            switch (assetType)
            {
                case AssetType.Crypto:
                    var coinDataList = await _cryptoTrackerLogic.GetCoinData(currency, [externalId]);
                    if (coinDataList.Count == 0) return null;
                    metadata = coinDataList.First();
                    break;
                case AssetType.Fiat:
                    metadata = await _currencyProvider.GetLatestRateAsync(currency, externalId);
                    break;
                case AssetType.Stock:
                    metadata = await _stockLogic.GetStockByIdAsync(currency, externalId);
                    break;
                default:
                    throw new InvalidOperationException($"Asset type {assetType} not supported");
            }

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
            var price = await _db.AssetPriceHistory.FirstOrDefaultAsync(p => p.Symbol == asset.Symbol && p.Currency == currency && p.Date == DateOnly.FromDateTime(DateTime.Now.Date));

            if (price == null)
            {
                price = new AssetPriceHistory()
                {
                    Symbol = asset.Symbol,
                    Date = DateOnly.FromDateTime(DateTime.Now),
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

        public async Task UpdateAllAssetMetadataAsync()
        {
            var assets = await _db.Assets.ToListAsync();
            _logger.LogTrace("Found {Count} assets", assets.Count);

            if (assets.Count == 0) return;

            var coinList = await _cryptoTrackerLogic.GetCoinList();
            _logger.LogTrace("Fetched {Count} coins", coinList.Count);

            foreach (var asset in assets.Where(x => x.AssetType == AssetType.Crypto))
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
                    _logger.LogTrace("Update name for '{Symbol}' to '{Name}'", asset.Symbol, coin.Value.Name);
                    asset.Name = coin.Value.Name;
                }

                if (string.IsNullOrWhiteSpace(asset.ExternalId))
                {
                    _logger.LogTrace("Update externalId for '{Symbol}' to '{ExternalId}'", asset.Symbol, coin.Value.Id);
                    asset.ExternalId = coin.Value.Id;
                }
            }
            await _db.SaveChangesAsync();

            var currencyList = await _currencyProvider.GetCurrenciesAsync();
            _logger.LogTrace("Fetched {Count} currencies", currencyList.Count());

            foreach (var asset in assets.Where(x => x.AssetType == AssetType.Fiat))
            {
                Currency? matchedCurrency = null;
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

            var foundExternalIds = await _db.Assets.Where(x => !string.IsNullOrWhiteSpace(x.ExternalId)).Select(x => new { x.ExternalId, x.AssetType }).ToListAsync();

            if (foundExternalIds.Count == 0) return;
            var currency = _config.BaseCurrency;
            var coinDataList = await _cryptoTrackerLogic.GetCoinData(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Crypto).Select(x => x.ExternalId!).ToList());
            var currencyDataList = await _currencyProvider.GetLatestRatesAsync(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Fiat).Select(x => x.ExternalId!).ToList());
            var stockDataList = await _stockLogic.GetStocksByIdsAsync(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Stock).Select(x => x.ExternalId!).ToList());

            var all = coinDataList.Union(currencyDataList).Union(stockDataList).ToList();

            foreach (var item in all)
            {
                await ApplyMetadataAsync(item);
            }

            await _db.SaveChangesAsync();
        }
    }
}
