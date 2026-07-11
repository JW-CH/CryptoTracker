using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using cryptotracker.webapi.Dtos;
using cryptotracker.webapi.Services;
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
        private readonly PortfolioQueryService _portfolioQueryService;
        private readonly PortfolioClock _clock;

        public CryptoTrackerController(ILogger<CryptoTrackerController> logger, PortfolioQueryService portfolioQueryService, PortfolioClock clock)
        {
            _logger = logger;
            _portfolioQueryService = portfolioQueryService;
            _clock = clock;
        }

        [HttpGet("measuring/date/{date}", Name = "GetMeasuringsByDate")]
        public async Task<List<AssetHoldingDto>> GetMeasuringsByDate([Required] DateTime date, string? symbol = null)
        {
            return await _portfolioQueryService.GetAssetDayMeasuringAsync(_clock.ToPortfolioDay(date), symbol);
        }

        [HttpGet("measuring/days/{days}", Name = "GetMeasuringsByDays")]
        public async Task<Dictionary<DateOnly, List<AssetHoldingDto>>> GetMeasuringsByDays([Required] int days = 7, string? symbol = null)
        {
            var dayList = new List<DateOnly>();
            var today = _clock.Today;
            for (int i = 0; i < days; i++)
            {
                dayList.Add(today.AddDays(-i));
            }

            var result = await _portfolioQueryService.GetAssetDayMeasuringBatchAsync(dayList, symbol);

            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        [HttpGet("standing/days/{days}", Name = "GetStandingsByDay")]
        public async Task<Dictionary<DateOnly, decimal>> GetStandingByDay([Required] int days = 7)
        {
            var today = _clock.Today;
            var dayList = new List<DateOnly>();
            for (int i = 0; i < days; i++)
            {
                dayList.Add(today.AddDays(-i));
            }

            var batchResult = await _portfolioQueryService.GetAssetDayMeasuringBatchAsync(dayList);

            return batchResult
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value.Sum(m => m.TotalValue));
        }

        [HttpGet("measuring", Name = "GetLatestMeasurings")]
        public async Task<List<AssetHoldingDto>> GetLatestMeasurings()
        {
            var today = _clock.Today;

            return await _portfolioQueryService.GetAssetDayMeasuringAsync(today);
        }

        [HttpGet("standing", Name = "GetLatestStanding")]
        public async Task<decimal> GetLatestStanding()
        {
            var today = _clock.Today;

            return (await _portfolioQueryService.GetAssetDayMeasuringAsync(today)).Sum(x => x.TotalValue);
        }
    }
}
