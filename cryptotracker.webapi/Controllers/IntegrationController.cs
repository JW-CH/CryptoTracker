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
    public class IntegrationController : ControllerBase
    {
        private readonly ILogger<CryptoTrackerController> _logger;
        private readonly DatabaseContext _db;
        private readonly CryptoTrackerLogic _cryptoTrackerLogic;

        public IntegrationController(ILogger<CryptoTrackerController> logger, DatabaseContext db, CryptoTrackerLogic cryptoTrackerLogic)
        {
            _logger = logger;
            _db = db;
            _cryptoTrackerLogic = cryptoTrackerLogic;
        }

        [HttpGet(Name = "GetIntegrations")]
        public List<IntegrationDetails> GetIntegrations()
        {
            return _db.ExchangeIntegrations.Select(IntegrationDetails.FromIntegration).ToList();
        }

        [HttpGet(Name = "GetIntegrationDetails")]
        public IntegrationDetails? GetIntegrationDetails([Required] Guid id)
        {
            var currency = "CHF";
            var integration = _db.ExchangeIntegrations.Include(x => x.AssetMeasurings).ThenInclude(x => x.Asset).FirstOrDefault(x => x.Id == id);

            if (integration == null) return null;
            var latest = integration.AssetMeasurings.Any() ? integration.AssetMeasurings?.Max(x => x.StandingDate.Date) : null;

            var latestMeasurings = integration.AssetMeasurings?.Where(x => x.StandingDate.Date == latest && x.StandingValue != 0).ToList() ?? new();

            return IntegrationDetails.FromIntegration(integration, latestMeasurings.Select(m => AssetMeasuringDto.FromModel(m, _db.AssetPriceHistory.FirstOrDefault(x => x.Date.Date == latest && x.Symbol == m.AssetId && x.Currency == currency)?.Price ?? 0)).ToList());
        }

        public struct IntegrationDetails
        {
            public required Guid Id { get; set; }
            public required string Name { get; set; }
            public string? Description { get; set; }
            public required bool IsManual { get; set; }
            public required bool IsHidden { get; set; }
            public required List<AssetMeasuringDto> Measurings { get; set; }

            public static IntegrationDetails FromIntegration(ExchangeIntegration integration) => FromIntegration(integration, new());

            public static IntegrationDetails FromIntegration(ExchangeIntegration integration, List<AssetMeasuringDto> measurings)
            {
                return new IntegrationDetails()
                {
                    Id = integration.Id,
                    Name = integration.Name,
                    Description = integration.Description,
                    IsManual = integration.IsManual,
                    IsHidden = integration.IsHidden,
                    Measurings = measurings ?? new()
                };
            }
        }
    }
}