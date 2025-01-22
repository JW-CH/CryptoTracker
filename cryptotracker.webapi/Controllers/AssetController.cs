using System.ComponentModel.DataAnnotations;
using cryptotracker.core.Logic;
using cryptotracker.database.Models;
using Microsoft.AspNetCore.Mvc;

namespace cryptotracker.webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AssetController : ControllerBase
    {
        private readonly ILogger<CryptoTrackerController> _logger;
        private readonly DatabaseContext _db;
        private readonly CryptoTrackerLogic _cryptoTrackerLogic;
        private readonly CryptoTrackerAssetLogic _cryptoTrackerAssetLogic;

        public AssetController(ILogger<CryptoTrackerController> logger, DatabaseContext db, CryptoTrackerLogic cryptoTrackerLogic)
        {
            _logger = logger;
            _db = db;
            _cryptoTrackerLogic = cryptoTrackerLogic;
            _cryptoTrackerAssetLogic = new CryptoTrackerAssetLogic(logger, cryptoTrackerLogic);
        }

        [HttpGet(Name = "GetAssets")]
        public List<Asset> GetAssets()
        {
            return _db.Assets.ToList();
        }

        [HttpGet(Name = "GetAsset")]
        public AssetData GetAsset([Required] string symbol)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");
            return new AssetData
            {
                Asset = asset,
                Price = _db.AssetPriceHistory.Where(x => x.Symbol == symbol).OrderByDescending(x => x.Date).FirstOrDefault()?.Price ?? 0
            };
        }

        [HttpGet(Name = "GetCoins")]
        public async Task<List<Coin>> GetCoins()
        {
            return await _cryptoTrackerLogic.GetCoinList();
        }

        [HttpGet(Name = "FindCoinsBySymbol")]
        public async Task<List<Coin>> FindCoinsBySymbol([Required] string symbol)
        {
            var coinList = await _cryptoTrackerLogic.GetCoinList();

            var list = coinList.Where(x => x.Symbol.ToLower() == symbol.ToLower()).ToList();

            return list;
        }

        [HttpGet(Name = "GetFiats")]
        public async Task<List<Fiat>> GetFiats()
        {
            return await _cryptoTrackerLogic.GetFiatList();
        }

        [HttpGet(Name = "FindFiatBySymbol")]
        public async Task<List<Fiat>> FindFiatBySymbol([Required] string symbol)
        {
            var fiatList = await _cryptoTrackerLogic.GetFiatList();

            return fiatList.Where(x => x.Symbol.ToLower() == symbol.ToLower()).ToList();
        }

        [HttpPost(Name = "SetExternalIdForSymbol")]
        public async Task<AssetData> SetExternalIdForSymbol([Required] string symbol, [FromBody] string externalId)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");

            using var tx = _db.Database.BeginTransaction();

            asset.ExternalId = externalId;
            _db.SaveChanges();

            var currency = "CHF";

            AssetMetadata metadata;
            if (asset.IsFiat)
            {
                var fiatDataList = await _cryptoTrackerLogic.GetFiatData(currency, [asset.ExternalId]);
                metadata = fiatDataList.FirstOrDefault();
            }
            else
            {
                var coinDataList = await _cryptoTrackerLogic.GetCoinData(currency, [asset.ExternalId]);
                metadata = coinDataList.FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(metadata.AssetId))
            {
                _cryptoTrackerAssetLogic.UpdateMetadataForAsset(_db, metadata, currency);
            }
            else
            {
                _logger.LogError($"Metadata not found for {asset.Symbol}");
            }

            _db.SaveChanges();
            tx.Commit();

            return new AssetData
            {
                Asset = asset,
                Price = _db.AssetPriceHistory.Where(x => x.Symbol == symbol).OrderByDescending(x => x.Date).FirstOrDefault()?.Price ?? 0
            }; ;
        }

        [HttpPost(Name = "SetVisibilityForSymbol")]
        public bool SetVisibilityForSymbol([Required] string symbol, [FromBody] bool isHidden)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");
            asset.IsHidden = isHidden;
            _db.SaveChanges();

            return true;
        }

        [HttpPost(Name = "SetFiatForSymbol")]
        public bool SetFiatForSymbol([Required] string symbol, [FromBody] bool isFiat)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");

            if (!string.IsNullOrEmpty(asset.ExternalId)) throw new Exception("Asset already has an external id and cannot be set as fiat");

            asset.IsFiat = isFiat;
            _db.SaveChanges();

            return true;
        }

        [HttpPost(Name = "AddAsset")]
        public async Task<bool> AddAsset([FromBody] AddAssetDto assetDto)
        {
            if (_db.Assets.Any(x => x.Symbol.ToLower() == assetDto.Symbol)) throw new Exception("Asset already exists");

            using var tx = _db.Database.BeginTransaction();

            var asset = new Asset
            {
                Symbol = assetDto.Symbol,
                ExternalId = assetDto.ExternalId,
                IsFiat = assetDto.IsFiat,
                IsHidden = false
            };

            _db.Assets.Add(asset);
            _db.SaveChanges();

            var currency = "CHF";

            AssetMetadata? metadata = null; ;
            if (asset.IsFiat)
            {
                var fiatDataList = await _cryptoTrackerLogic.GetFiatData(currency, [asset.ExternalId]);
                metadata = fiatDataList.FirstOrDefault();
            }
            else
            {
                var coinDataList = await _cryptoTrackerLogic.GetCoinData(currency, [asset.ExternalId]);
                metadata = coinDataList.FirstOrDefault();
            }

            if (metadata.HasValue)
            {
                _cryptoTrackerAssetLogic.UpdateMetadataForAsset(_db, metadata.Value, currency);
            }
            else
            {
                throw new Exception($"Metadata not found for {asset.Symbol}");
            }

            _db.SaveChanges();
            tx.Commit();

            return true;
        }

        [HttpPost(Name = "ResetAsset")]
        public bool ResetAsset([FromBody] string symbol)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");

            _db.AssetPriceHistory.RemoveRange(_db.AssetPriceHistory.Where(x => x.Asset == asset));

            asset.ExternalId = "";
            asset.Name = "";
            asset.Image = "";
            _db.SaveChanges();

            return true;
        }

        public struct AddAssetDto
        {
            public string Symbol { get; set; }
            public bool IsFiat { get; set; }
            public string ExternalId { get; set; }
        }

        public struct AssetData
        {
            public required Asset Asset { get; set; }
            public required decimal Price { get; set; }
        }
    }
}