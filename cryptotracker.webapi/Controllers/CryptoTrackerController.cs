using cryptotracker.core.Models;
using cryptotracker.database.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet(Name = "GetMeasuringsByDay")]
        public async Task<Dictionary<DateTime, List<AssetMeasuringDto>>> GetMeasuringsByDay(int days = 7)
        {
            var dayList = _db.AssetMeasurings.Include(x => x.Asset).Where(x => x.StandingDate.Date >= DateTime.Now.AddDays(days * -1)).GroupBy(x => x.StandingDate.Date);

            return dayList.ToDictionary(x => x.Key, x => x.Select(a => AssetMeasuringDto.FromModel(a)).ToList());
        }
    }
}
