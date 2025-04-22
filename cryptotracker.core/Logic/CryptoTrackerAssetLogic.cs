using cryptotracker.database.Models;
using Microsoft.Extensions.Logging;

namespace cryptotracker.core.Logic
{
    public class CryptoTrackerAssetLogic
    {
        private ILogger _logger;
        private readonly CryptoTrackerLogic _cryptoTrackerLogic;
        private readonly IFiatLogic _fiatLogic;
        private readonly IStockLogic _stockLogic;

        public CryptoTrackerAssetLogic(ILogger logger, CryptoTrackerLogic cryptoTrackerLogic, IFiatLogic fiatLogic, IStockLogic stockLogic)
        {
            _logger = logger;
            _cryptoTrackerLogic = cryptoTrackerLogic;
            _fiatLogic = fiatLogic;
            _stockLogic = stockLogic;
        }

        public void UpdateMetadataForAsset(DatabaseContext db, AssetMetadata metadata)
        {
            var asset = db.Assets.FirstOrDefault(a => a.ExternalId == metadata.AssetId);

            if (asset == null) return;

            if (string.IsNullOrWhiteSpace(asset.Name))
                asset.Name = metadata.Name;

            if (string.IsNullOrWhiteSpace(asset.Image))
                asset.Image = metadata.Image;

            var price = db.AssetPriceHistory.FirstOrDefault(p => p.Symbol == asset.Symbol && p.Date == DateTime.Today);

            if (price == null)
            {
                price = new AssetPriceHistory()
                {
                    Symbol = asset.Symbol,
                    Date = DateTime.Today,
                    Currency = metadata.Currency,
                    Price = metadata.Price,
                };

                _logger.LogTrace($"Add AssetPriceHistory for {price.Symbol}, {price.Date} - {price.Price} {price.Currency}");

                db.AssetPriceHistory.Add(price);
            }
            else
            {
                _logger.LogTrace($"Update AssetPriceHistory for {price.Symbol}, {price.Date} from {price.Price} {price.Currency} to {metadata.Price} {price.Currency}");
                if (price.Currency != metadata.Currency)
                {
                    _logger.LogTrace($"Update AssetPriceHistory currency for {price.Symbol}, {price.Date} from {price.Currency} to {metadata.Currency}");

                    db.AssetPriceHistory.Remove(price);

                    price = new AssetPriceHistory()
                    {
                        Symbol = asset.Symbol,
                        Date = DateTime.Today,
                        Currency = metadata.Currency,
                        Price = metadata.Price,
                    };
                    db.AssetPriceHistory.Add(price);
                }
                else
                {
                    price.Price = metadata.Price;
                }
            }
        }

        public async Task UpdateAllAssetMetadata(DatabaseContext db)
        {
            var assets = db.Assets.ToList();
            _logger.LogTrace($"Found {assets.Count} assets");

            if (assets.Count == 0) return;

            var coinList = _cryptoTrackerLogic.GetCoinList().Result;
            _logger.LogTrace($"Fetched {coinList.Count()} coins");

            if (coinList != null)
            {
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
                        _logger.LogTrace($"Update name for '{asset.Symbol}' to '{coin.Value.Name}'");
                        asset.Name = coin.Value.Name;
                    }

                    if (string.IsNullOrWhiteSpace(asset.ExternalId))
                    {
                        _logger.LogTrace($"Update externalId for '{asset.Symbol}' to '{coin.Value.Id}'");
                        asset.ExternalId = coin.Value.Id;
                    }
                }
                db.SaveChanges();
            }

            var fiatList = _fiatLogic.GetFiatList().Result;
            _logger.LogTrace($"Fetched {fiatList.Count()} fiats");

            if (fiatList != null)
            {
                foreach (var asset in assets.Where(x => x.AssetType == AssetType.Fiat))
                {
                    Fiat? fiat = null;
                    if (string.IsNullOrWhiteSpace(asset.ExternalId))
                    {
                        var fiats = fiatList.Where(x => x.Symbol.ToLower() == asset.Symbol.ToLower());

                        if (fiats.Count() != 1) continue;

                        fiat = fiats.First();
                    }
                    else
                    {
                        fiat = fiatList.FirstOrDefault(x => x.Symbol.ToLower() == asset.ExternalId.ToLower());
                    }

                    if (fiat == null) continue;

                    if (string.IsNullOrWhiteSpace(asset.Name))
                    {
                        _logger.LogTrace($"Update name for '{asset.Symbol}' to '{fiat.Value.Name}'");
                        asset.Name = fiat.Value.Name;
                    }
                    if (string.IsNullOrWhiteSpace(asset.ExternalId))
                    {

                        _logger.LogTrace($"Update externalId for '{asset.Symbol}' to '{fiat.Value.Symbol}'");
                        asset.ExternalId = fiat.Value.Symbol;
                    }
                }
                db.SaveChanges();
            }

            var foundExternalIds = db.Assets.Where(x => !string.IsNullOrWhiteSpace(x.ExternalId)).Select(x => new { x.ExternalId, x.AssetType }).ToList();

            if (foundExternalIds.Count == 0) return;
            var currency = "chf";
            var coinDataList = await _cryptoTrackerLogic.GetCoinData(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Crypto).Select(x => x.ExternalId!).ToList());
            var fiatDataList = await _fiatLogic.GetFiatsByIdsAsync(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Fiat).Select(x => x.ExternalId!).ToList());
            var stockDataList = await _stockLogic.GetStocksByIdsAsync(currency, foundExternalIds.Where(x => x.AssetType == AssetType.Stock).Select(x => x.ExternalId!).ToList());

            var all = coinDataList.Union(fiatDataList).Union(stockDataList).ToList();

            foreach (var item in all)
            {
                UpdateMetadataForAsset(db, item);
            }

            db.SaveChanges();
        }
    }
}