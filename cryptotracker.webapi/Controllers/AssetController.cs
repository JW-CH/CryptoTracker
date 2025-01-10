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

        public AssetController(ILogger<CryptoTrackerController> logger, DatabaseContext db, CryptoTrackerLogic cryptoTrackerLogic)
        {
            _logger = logger;
            _db = db;
            _cryptoTrackerLogic = cryptoTrackerLogic;
        }

        [HttpGet(Name = "GetAssets")]
        public List<Asset> GetAssets()
        {
            return _db.Assets.ToList();
        }

        [HttpGet("{symbol}", Name = "GetAsset")]
        public AssetData GetAsset(string symbol)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");
            return new AssetData
            {
                Asset = asset,
                Price = _db.AssetPriceHistory.Where(x => x.Symbol == symbol).OrderByDescending(x => x.Date).FirstOrDefault()?.Price ?? 0
            };
        }

        [HttpGet("{symbol}", Name = "FindCoinsBySymbol")]
        public async Task<List<Coin>> FindCoinsBySymbol(string symbol)
        {
            var coinList = await _cryptoTrackerLogic.GetCoinList();

            var list = coinList.Where(x => x.Symbol.ToLower() == symbol.ToLower()).ToList();

            return list;
        }

        [HttpGet("{symbol}", Name = "FindFiatBySymbol")]
        public async Task<List<Fiat>> FindFiatBySymbol(string symbol)
        {
            var fiatList = await _cryptoTrackerLogic.GetFiatList();

            return fiatList.Where(x => x.Symbol.ToLower() == symbol.ToLower()).ToList();
        }

        [HttpPost("{symbol}", Name = "SetAssetForSymbol")]
        public bool SetAssetForSymbol(string symbol, [FromBody] string externalId)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");
            asset.ExternalId = externalId;
            _db.SaveChanges();

            return true;
        }

        [HttpPost("{symbol}", Name = "SetVisibilityForSymbol")]
        public bool SetVisibilityForSymbol(string symbol, [FromBody] bool isHidden)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");
            asset.IsHidden = isHidden;
            _db.SaveChanges();

            return true;
        }

        [HttpPost("{symbol}", Name = "SetFiatForSymbol")]
        public bool SetFiatForSymbol(string symbol, [FromBody] bool isFiat)
        {
            var asset = _db.Assets.FirstOrDefault(x => x.Symbol == symbol) ?? throw new Exception("Asset not found");

            if (!string.IsNullOrEmpty(asset.ExternalId)) throw new Exception("Asset already has an external id and cannot be set as fiat");

            asset.IsFiat = isFiat;
            _db.SaveChanges();

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

        public struct AssetData
        {
            [Required]
            public Asset Asset { get; set; }
            [Required]
            public decimal Price { get; set; }
        }
    }
}