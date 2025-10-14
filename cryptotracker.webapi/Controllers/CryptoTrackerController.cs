using System.ComponentModel.DataAnnotations;
using cryptotracker.core.Interfaces;
using cryptotracker.database.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cryptotracker.webapi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CryptoTrackerController : ControllerBase
    {
        private readonly ILogger<CryptoTrackerController> _logger;
        private readonly DatabaseContext _db;
        private readonly ICryptoTrackerConfig _config;

        public CryptoTrackerController(ILogger<CryptoTrackerController> logger, DatabaseContext db, ICryptoTrackerConfig config)
        {
            _logger = logger;
            _db = db;
            _config = config;
        }

        [HttpGet("measuring/date/{date}", Name = "GetMeasuringsByDate")]
        public List<MessungDto> GetMeasuringsByDate([Required] DateTime date, string? symbol = null)
        {
            return ApiHelper.GetAssetDayMeasuring(_db, date.ToLocalTime(), symbol);
        }

        [HttpGet("measuring/days/{days}", Name = "GetMeasuringsByDays")]
        public Dictionary<DateTime, List<MessungDto>> GetMeasuringsByDays([Required] int days = 7, string? symbol = null)
        {
            var dayList = new List<DateTime>();
            for (int i = 0; i < days; i++)
            {
                DateTime date = DateTime.Today.AddDays(-i);
                dayList.Add(date.Date);
            }

            var result = new Dictionary<DateTime, List<MessungDto>>();
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

        [HttpGet("standing/days/{days}", Name = "GetStandingsByDay")]
        public Dictionary<DateTime, decimal> GetStandingByDay([Required] int days = 7)
        {
            var dayList = new List<DateTime>();
            for (int i = 0; i < days; i++)
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

        [HttpGet("measuring", Name = "GetLatestMeasurings")]
        public List<MessungDto> GetLatestMeasurings()
        {
            var day = DateTime.Today;

            return ApiHelper.GetAssetDayMeasuring(_db, day);
        }

        [HttpGet("standing", Name = "GetLatestStanding")]
        public decimal GetLatestStanding()
        {
            var day = DateTime.Today;

            return ApiHelper.GetAssetDayMeasuring(_db, day).Sum(x => x.TotalValue);
        }
    }
}
