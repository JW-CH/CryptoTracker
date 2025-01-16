using cryptotracker.core.Models;
using cryptotracker.database.DTOs;
using cryptotracker.database.Models;
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
        public Dictionary<DateTime, List<AssetMeasuringDto>> GetMeasuringsByDay(int days = 7, string? symbol = null)
        {
            //var dayList = _db.AssetMeasurings.Include(x => x.Asset).Where(x => x.StandingDate.Date >= DateTime.Now.AddDays(days * -1)).GroupBy(x => x.StandingDate.Date);

            var dayList = new List<DateTime>();
            for (int i = 0; i < 7; i++)
            {
                DateTime date = DateTime.Today.AddDays(-i);
                dayList.Add(date.Date);
            }

            var result = new Dictionary<DateTime, List<AssetMeasuringDto>>();
            foreach (var day in dayList.ToList())
            {
                if (symbol != null)
                {
                    result[day] = GetAssetDayMeasuring(day, symbol).ToList();
                }
                else
                {
                    result[day] = GetAssetDayMeasuring(day);
                }
            }

            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        [HttpGet("{days}", Name = "GetStandingsByDay")]
        public Dictionary<DateTime, decimal> GetStandingByDay(int days = 7)
        {
            // var dayList = _db.AssetMeasurings.Include(x => x.Asset).Where(x => x.StandingDate.Date >= DateTime.Now.AddDays(days * -1)).GroupBy(x => x.StandingDate.Date);

            var dayList = new List<DateTime>();
            for (int i = 0; i < 7; i++)
            {
                DateTime date = DateTime.Today.AddDays(-i);
                dayList.Add(date.Date);
            }

            var result = new Dictionary<DateTime, decimal>();
            foreach (var day in dayList.ToList())
            {
                result[day] = GetAssetDayMeasuring(day).Sum(x => x.TotalValue);
            }

            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
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

            return GetAssetDayMeasuring(day).Sum(x => x.TotalValue);
        }

        private List<AssetMeasuringDto> GetAssetDayMeasuring(DateTime day)
        {
            return GetAssetDayMeasuring(day, _db.Assets.Where(x => !x.IsHidden).ToList());
        }

        private List<AssetMeasuringDto> GetAssetDayMeasuring(DateTime day, string symbol)
        {
            return GetAssetDayMeasuring(day, _db.Assets.Where(x => x.Symbol.ToLower() == symbol.ToLower()).ToList());
        }
        private List<AssetMeasuringDto> GetAssetDayMeasuring(DateTime day, List<Asset> assets)
        {
            var result = new List<AssetMeasuringDto>();

            var allSymbols = assets.Select(x => x.Symbol).ToList();

            var currency = "chf";
            var integrations = _db.ExchangeIntegrations.ToList();
            var priceHistories = _db.AssetPriceHistory
                .Where(x => x.Date <= day.Date && x.Currency == currency)
                .Where(x => allSymbols.Contains(x.Symbol))
                .OrderByDescending(x => x.Date)
                .ToList();

            var assetMeasurings = _db.AssetMeasurings
                .Where(x => x.StandingDate.Date <= day.Date)
                .Where(x => allSymbols.Contains(x.AssetId))
                .OrderByDescending(x => x.StandingDate)
                .ToList();

            foreach (var asset in assets)
            {
                var priceHistory = priceHistories
                    .FirstOrDefault(x => x.Symbol == asset.Symbol);

                var allMeasurings = new List<AssetMeasuring>();
                foreach (var integration in integrations)
                {
                    var datt = assetMeasurings
                        .Where(x => x.IntegrationId == integration.Id && x.AssetId == asset.Symbol)
                        .FirstOrDefault()?.StandingDate.Date;

                    if (!datt.HasValue) continue;

                    var measurings = assetMeasurings
                        .Where(x => x.StandingDate.Date == datt && x.IntegrationId == integration.Id && x.AssetId == asset.Symbol)
                        .ToList();

                    allMeasurings.AddRange(measurings);
                }

                var dto = AssetMeasuringDto.SumFromModels(asset, allMeasurings, priceHistory?.Price ?? 0m);

                result.Add(dto);
            }

            return result;
        }
    }
}
