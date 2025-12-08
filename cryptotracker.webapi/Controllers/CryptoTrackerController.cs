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
            return ApiHelper.GetAssetDayMeasuring(_db, DateOnly.FromDateTime(date.ToLocalTime()), symbol);
        }

        [HttpGet("measuring/days/{days}", Name = "GetMeasuringsByDays")]
        public Dictionary<DateOnly, List<MessungDto>> GetMeasuringsByDays([Required] int days = 7, string? symbol = null)
        {
            var dayList = new List<DateOnly>();
            var today = DateOnly.FromDateTime(DateTime.Now);
            for (int i = 0; i < days; i++)
            {
                DateOnly date = today.AddDays(-i);
                dayList.Add(date);
            }

            var result = new Dictionary<DateOnly, List<MessungDto>>();
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
        public Dictionary<DateOnly, decimal> GetStandingByDay([Required] int days = 7)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var dayList = new List<DateOnly>();
            for (int i = 0; i < days; i++)
            {
                DateOnly date = today.AddDays(-i);
                dayList.Add(date);
            }

            var result = new Dictionary<DateOnly, decimal>();
            foreach (var day in dayList.ToList())
            {
                result[day] = ApiHelper.GetAssetDayMeasuring(_db, day).Sum(x => x.TotalValue);
            }

            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        [HttpGet("measuring", Name = "GetLatestMeasurings")]
        public List<MessungDto> GetLatestMeasurings()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            return ApiHelper.GetAssetDayMeasuring(_db, today);
        }

        [HttpGet("standing", Name = "GetLatestStanding")]
        public decimal GetLatestStanding()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            return ApiHelper.GetAssetDayMeasuring(_db, today).Sum(x => x.TotalValue);
        }
    }
}
