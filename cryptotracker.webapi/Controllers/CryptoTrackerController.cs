using Coinbase.Net.Clients;
using CryptoExchange.Net.Authentication;
using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using ImmichFrame.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace cryptotracker.webapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
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

        [HttpGet(Name = "Get")]
        public async Task<Dictionary<string, decimal>> Get()
        {
            var today = _db.AssetMeasurings.Where(x=>x.StandingDate.Date == DateTime.Today).ToList();
            Dictionary<string, decimal> result = new();
            foreach (var item in today) {

                if (result.ContainsKey(item.AssetId))
                {
                    result[item.AssetId] += item.StandingValue;
                }
                else {
                    result[item.AssetId] = item.StandingValue;
                }
            }

            return result;
        }
    }
}
