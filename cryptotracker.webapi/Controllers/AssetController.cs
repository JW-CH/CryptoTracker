using System.ComponentModel.DataAnnotations;
using cryptotracker.core.Logic;
using cryptotracker.core.Models;
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
        private readonly CryptotrackerConfig _config;

        public AssetController(ILogger<CryptoTrackerController> logger, DatabaseContext db, CryptotrackerConfig config)
        {
            _logger = logger;
            _db = db;
            _config = config;
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
            var coinList = await CryptoTrackerLogic.GetCoinList();

            return coinList.Where(x => x.symbol.ToLower().Contains(symbol.ToLower())).ToList();
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
            asset.IsFiat = isFiat;
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