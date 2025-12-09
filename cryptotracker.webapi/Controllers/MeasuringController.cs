using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using cryptotracker.core.Logic;
using cryptotracker.database.DTOs;
using cryptotracker.database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MeasuringController : ControllerBase
    {
        private readonly ILogger<CryptoTrackerController> _logger;
        private readonly DatabaseContext _db;
        private readonly ICryptoTrackerLogic _cryptoTrackerLogic;

        public MeasuringController(ILogger<CryptoTrackerController> logger, DatabaseContext db, ICryptoTrackerLogic cryptoTrackerLogic)
        {
            _logger = logger;
            _db = db;
            _cryptoTrackerLogic = cryptoTrackerLogic;
        }

        [HttpGet("{id}", Name = "GetMeasuringsByIntegration")]
        public List<AssetMeasuringDto> GetMeasuringsByIntegration([Required] Guid id)
        {
            return _db.AssetMeasurings.Where(x => x.IntegrationId == id).Select(AssetMeasuringDto.FromModel).ToList();
        }

        [HttpPost(Name = "AddIntegrationMeasuring")]
        public async Task<bool> AddIntegrationMeasuring([Required] Guid id, [FromBody] AddMeasuringDto dto)
        {
            var integration = await _db.ExchangeIntegrations.FindAsync(id);

            if (integration == null) throw new Exception("Integration nicht gefunden");

            if (!integration.IsManual) throw new Exception("Integration ist nicht manuell");

            var asset = await _db.Assets.FindAsync(dto.Symbol);

            if (asset == null) throw new Exception("Asset nicht gefunden");

            var today = dto.Date.Date;
            var tomorrow = today.AddDays(1);

            AssetMeasuring? measuring = await _db.AssetMeasurings.FirstOrDefaultAsync(x => x.Symbol == dto.Symbol && x.IntegrationId == integration.Id && x.Timestamp >= today && x.Timestamp < tomorrow);

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
                await _db.AssetMeasurings.AddAsync(measuring);
            }

            await _db.SaveChangesAsync();

            return true;
        }

        [HttpDelete("{id}", Name = "DeleteMeasuringById")]
        public async Task<bool> DeleteMeasuringById([Required] Guid id)
        {
            var measuring = await _db.AssetMeasurings.Include(x => x.Integration).FirstOrDefaultAsync(x => x.Id == id);

            if (measuring == null) throw new Exception("Measuring not found");

            if (!measuring.Integration.IsManual) throw new Exception("Integration is not manual");

            _db.AssetMeasurings.Remove(measuring);

            await _db.SaveChangesAsync();

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