using System.ComponentModel.DataAnnotations;
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

        public struct AssetData
        {
            [Required]
            public Asset Asset { get; set; }
            [Required]
            public decimal Price { get; set; }
        }
    }
}