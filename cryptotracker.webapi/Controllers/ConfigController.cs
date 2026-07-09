using cryptotracker.core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cryptotracker.webapi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ICryptoTrackerConfig _config;

        public ConfigController(ICryptoTrackerConfig config)
        {
            _config = config;
        }

        [HttpGet(Name = "GetConfig")]
        public ConfigResponse GetConfig()
        {
            return new ConfigResponse(_config.BaseCurrency);
        }

        public record ConfigResponse(string BaseCurrency);
    }
}
