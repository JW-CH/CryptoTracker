using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
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
        public async Task<List<MessungDto>> GetMeasuringsByDate([Required] DateTime date, string? symbol = null)
        {
            return await ApiHelper.GetAssetDayMeasuring(_db, DateOnly.FromDateTime(date.ToLocalTime()), symbol);
        }

        [HttpGet("measuring/days/{days}", Name = "GetMeasuringsByDays")]
        public async Task<Dictionary<DateOnly, List<MessungDto>>> GetMeasuringsByDays([Required] int days = 7, string? symbol = null)
        {
            var dayList = new List<DateOnly>();
            var today = DateOnly.FromDateTime(DateTime.Now);
            for (int i = 0; i < days; i++)
            {
                DateOnly date = today.AddDays(-i);
                dayList.Add(date);
            }

            var tasks = dayList.Select(async day => (day, data: await ApiHelper.GetAssetDayMeasuring(_db, day, symbol)));
            var results = await Task.WhenAll(tasks);
            return results.OrderBy(x => x.day).ToDictionary(x => x.day, x => x.data.ToList());
        }

        [HttpGet("standing/days/{days}", Name = "GetStandingsByDay")]
        public async Task<Dictionary<DateOnly, decimal>> GetStandingByDay([Required] int days = 7)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var dayList = new List<DateOnly>();
            for (int i = 0; i < days; i++)
            {
                DateOnly date = today.AddDays(-i);
                dayList.Add(date);
            }

            var tasks = dayList.Select(async day => (day, totalValue: (await ApiHelper.GetAssetDayMeasuring(_db, day)).Sum(x => x.TotalValue)));
            var results = await Task.WhenAll(tasks);
            return results.OrderBy(x => x.day).ToDictionary(x => x.day, x => x.totalValue);
        }

        [HttpGet("measuring", Name = "GetLatestMeasurings")]
        public async Task<List<MessungDto>> GetLatestMeasurings()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            return await ApiHelper.GetAssetDayMeasuring(_db, today);
        }

        [HttpGet("standing", Name = "GetLatestStanding")]
        public async Task<decimal> GetLatestStanding()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            return (await ApiHelper.GetAssetDayMeasuring(_db, today)).Sum(x => x.TotalValue);
        }
    }
}
