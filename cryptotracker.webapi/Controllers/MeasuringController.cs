using System.ComponentModel.DataAnnotations;
using cryptotracker.core.Logic;
using cryptotracker.database.DTOs;
using cryptotracker.database.Models;
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
        public List<AssetMeasuringDto> GetMeasuringsByIntegration([Required] Guid id)
        {
            return _db.AssetMeasurings.Where(x => x.IntegrationId == id).Select(AssetMeasuringDto.FromModel).ToList();
        }

        [HttpPost(Name = "AddIntegrationMeasuring")]
        public bool AddIntegrationMeasuring([Required] Guid id, [FromBody] AddMeasuringDto dto)
        {
            var integration = _db.ExchangeIntegrations.Find(id);

            if (integration == null) throw new Exception("Integration nicht gefunden");

            if (!integration.IsManual) throw new Exception("Integration ist nicht manuell");

            var asset = _db.Assets.Find(dto.Symbol);

            if (asset == null) throw new Exception("Asset nicht gefunden");

            AssetMeasuring? measuring = _db.AssetMeasurings.Where(x => x.Symbol == dto.Symbol && x.IntegrationId == integration.Id && x.Timestamp.Date == dto.Date.Date).FirstOrDefault();

            if (measuring != null)
            {
                measuring.Timestamp = dto.Date;
                measuring.Amount = dto.Amount;
            }
            else
            {
                measuring = new AssetMeasuring()
                {
                    Symbol = asset.Symbol,
                    IntegrationId = integration.Id,
                    Timestamp = dto.Date,
                    Amount = dto.Amount
                };
                _db.AssetMeasurings.Add(measuring);
            }

            _db.SaveChanges();

            return true;
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

    public struct AddMeasuringDto
    {
        public string Symbol { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}