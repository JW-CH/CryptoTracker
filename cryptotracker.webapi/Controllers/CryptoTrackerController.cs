using cryptotracker.core.Models;
using cryptotracker.database.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CryptoTrackerController : ControllerBase
    {
        private readonly ILogger<CryptoTrackerController> _logger;
        private readonly DatabaseContext _db;
        private readonly CryptotrackerConfig _config;

        public CryptoTrackerController(ILogger<CryptoTrackerController> logger, DatabaseContext db, CryptotrackerConfig config)
        {
            _logger = logger;
            _db = db;
            _config = config;
        }

        [HttpGet("{days}", Name = "GetMeasuringsByDay")]
        public Dictionary<DateTime, List<AssetMeasuringDto>> GetMeasuringsByDay(int days = 7)
        {
            var dayList = _db.AssetMeasurings.Include(x => x.Asset).Where(x => x.StandingDate.Date >= DateTime.Now.AddDays(days * -1)).GroupBy(x => x.StandingDate.Date);

            var result = new Dictionary<DateTime, List<AssetMeasuringDto>>();
            foreach (var day in dayList.ToList())
            {
                result[day.Key] = GetAssetDayMeasuring(day.Key);
            }

            return result;
        }

        [HttpGet("{days}", Name = "GetStandingsByDay")]
        public Dictionary<DateTime, decimal> GetStandingByDay(int days = 7)
        {
            var dayList = _db.AssetMeasurings.Include(x => x.Asset).Where(x => x.StandingDate.Date >= DateTime.Now.AddDays(days * -1)).GroupBy(x => x.StandingDate.Date);

            var result = new Dictionary<DateTime, decimal>();
            foreach (var day in dayList.ToList())
            {
                result[day.Key] = GetAssetDayMeasuring(day.Key).Sum(x => x.FiatValue);
            }

            return result;
        }

        [HttpGet(Name = "GetLatestMeasurings")]
        public List<AssetMeasuringDto> GetLatestMeasurings()
        {
            var day = _db.AssetMeasurings.Max(x => x.StandingDate.Date);

            return GetAssetDayMeasuring(day);
        }

        [HttpGet(Name = "GetLatestStanding")]
        public decimal GetLatestStanding()
        {
            var day = _db.AssetMeasurings.Max(x => x.StandingDate.Date);

            return GetAssetDayMeasuring(day).Sum(x => x.FiatValue);
        }

        private List<AssetMeasuringDto> GetAssetDayMeasuring(DateTime day)
        {
            var result = new List<AssetMeasuringDto>();

            var currency = "chf";
            foreach (var asset in _db.Assets.ToList())
            {
                var price = _db.AssetPriceHistory.Where(x => x.Date == day.Date && x.Symbol == asset.Symbol && x.Currency == currency).FirstOrDefault()?.Price ?? 0m;
                var amount = _db.AssetMeasurings.Where(x => x.StandingDate.Date == day.Date && x.AssetId == asset.Symbol).Sum(x => x.StandingValue);
                var dto = new AssetMeasuringDto
                {
                    AssetId = asset.Symbol,
                    AssetName = asset.Name,
                    AssetAmount = amount,
                    AssetPrice = price,
                    FiatValue = amount * price
                };

                result.Add(dto);
            }

            return result;
        }
    }
}
