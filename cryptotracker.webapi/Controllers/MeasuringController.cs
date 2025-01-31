using System.ComponentModel.DataAnnotations;
using cryptotracker.core.Logic;
using cryptotracker.database.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class MeasuringController : ControllerBase
    {
        private readonly ILogger<CryptoTrackerController> _logger;
        private readonly DatabaseContext _db;
        private readonly CryptoTrackerLogic _cryptoTrackerLogic;

        public MeasuringController(ILogger<CryptoTrackerController> logger, DatabaseContext db, CryptoTrackerLogic cryptoTrackerLogic)
        {
            _logger = logger;
            _db = db;
            _cryptoTrackerLogic = cryptoTrackerLogic;
        }

        [HttpGet(Name = "GetMeasuringsByIntegration")]
        public List<AssetMeasurementDto> GetMeasuringsByIntegration([Required] Guid id)
        {
            return _db.AssetMeasurings.Where(x => x.IntegrationId == id).Select(AssetMeasurementDto.FromModel).ToList();
        }

        [HttpPost(Name = "DeleteMeasuringById")]
        public bool DeleteMeasuringById([FromBody] Guid id)
        {
            var measuring = _db.AssetMeasurings.Include(x => x.Integration).Where(x => x.Id == id).FirstOrDefault();

            if (measuring == null) throw new Exception("Measuring not found");

            if (!measuring.Integration.IsManual) throw new Exception("Integration is not manual");

            _db.AssetMeasurings.Remove(measuring);

            _db.SaveChanges();

            return true;
        }
    }
}