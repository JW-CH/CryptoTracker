using System.ComponentModel.DataAnnotations;
using cryptotracker.core.Models;
using cryptotracker.database.DTOs;
using Microsoft.AspNetCore.Mvc;

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


        [HttpGet(Name = "GetIntegrationMeasuringByDay")]
        public object GetIntegrationMeasuringByDay(string? symbol = null)
        {
            return null;
        }

        [HttpGet(Name = "GetMeasuringsByDay")]
        public Dictionary<DateTime, List<AssetMeasuringDto>> GetMeasuringsByDay([Required] int days = 7, string? symbol = null)
        {
            var dayList = new List<DateTime>();
            for (int i = 0; i < days; i++)
            {
                DateTime date = DateTime.Today.AddDays(-i);
                dayList.Add(date.Date);
            }

            var result = new Dictionary<DateTime, List<AssetMeasuringDto>>();
            foreach (var day in dayList.ToList())
            {
                if (symbol != null)
                {
                    result[day] = ApiHelper.GetAssetDayMeasuring(_db, day, symbol).ToList();
                }
                else
                {
                    result[day] = ApiHelper.GetAssetDayMeasuring(_db, day);
                }
            }

            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        [HttpGet(Name = "GetStandingsByDay")]
        public Dictionary<DateTime, decimal> GetStandingByDay([Required] int days = 7)
        {
            var dayList = new List<DateTime>();
            for (int i = 0; i < 7; i++)
            {
                DateTime date = DateTime.Today.AddDays(-i);
                dayList.Add(date.Date);
            }

            var result = new Dictionary<DateTime, decimal>();
            foreach (var day in dayList.ToList())
            {
                result[day] = ApiHelper.GetAssetDayMeasuring(_db, day).Sum(x => x.TotalValue);
            }

            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        [HttpGet(Name = "GetLatestMeasurings")]
        public List<AssetMeasuringDto> GetLatestMeasurings()
        {
            var day = _db.AssetMeasurings.Max(x => x.Timestamp.Date);

            return ApiHelper.GetAssetDayMeasuring(_db, day);
        }

        [HttpGet(Name = "GetLatestStanding")]
        public decimal GetLatestStanding()
        {
            var day = _db.AssetMeasurings.Max(x => x.Timestamp.Date);

            return ApiHelper.GetAssetDayMeasuring(_db, day).Sum(x => x.TotalValue);
        }
    }
}
