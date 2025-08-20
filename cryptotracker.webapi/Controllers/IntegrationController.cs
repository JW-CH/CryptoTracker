using System.ComponentModel.DataAnnotations;
using cryptotracker.core.Logic;
using cryptotracker.database.DTOs;
using cryptotracker.database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        public List<IntegrationDto> GetIntegrations()
        {
            return _db.ExchangeIntegrations.Select(IntegrationDto.FromModel).ToList();
        }

        [HttpGet("{id}/detail", Name = "GetIntegrationDetails")]
        public IntegrationDetails? GetIntegrationDetails([Required] Guid id)
        {
            var integration = _db.ExchangeIntegrations.Include(x => x.AssetMeasurings).ThenInclude(x => x.Asset).FirstOrDefault(x => x.Id == id);

            if (integration == null) return null;

            var measurings = ApiHelper.GetAssetDayMeasuring(_db, DateTime.Today, integrationId: integration.Id);

            return IntegrationDetails.FromIntegration(integration, measurings);
        }

        [HttpPost(Name = "AddIntegration")]
        public bool AddIntegration([FromBody] AddIntegrationDto dto)
        {
            if (_db.ExchangeIntegrations.Any(x => x.Name.ToLower() == dto.Name.ToLower())) throw new Exception("Integration mit diesem Namen existiert bereits.");

            var integration = new ExchangeIntegration
            {
                Name = dto.Name,
                Description = dto.Description,
                IsHidden = false,
                IsManual = true,
            };

            _db.Add(integration);
            _db.SaveChanges();

            return true;
        }

        public struct AddIntegrationDto
        {
            public string Name { get; set; }
            public string? Description { get; set; }
        }

        public struct IntegrationDetails
        {
            public required IntegrationDto Integration { get; set; }
            public required List<MessungDto> Measurings { get; set; }

            public static IntegrationDetails FromIntegration(ExchangeIntegration integration) => FromIntegration(integration, new());

            public static IntegrationDetails FromIntegration(ExchangeIntegration integration, List<MessungDto> measurings)
            {
                return new IntegrationDetails()
                {
                    Integration = IntegrationDto.FromModel(integration),
                    Measurings = measurings ?? new()
                };
            }
        }
    }
}