using System.ComponentModel.DataAnnotations;
using cryptotracker.webapi.Dtos;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static cryptotracker.webapi.Services.IntegrationService;

namespace cryptotracker.webapi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class IntegrationController : ControllerBase
    {
        private readonly IntegrationService _integrationService;
        private readonly MeasuringService _measuringService;

        public IntegrationController(IntegrationService integrationService, MeasuringService measuringService)
        {
            _integrationService = integrationService;
            _measuringService = measuringService;
        }

        [HttpGet(Name = "GetIntegrations")]
        public async Task<List<IntegrationDto>> GetIntegrations()
        {
            return await _integrationService.GetIntegrationsAsync();
        }

        [HttpGet("{id}/detail", Name = "GetIntegrationDetails")]
        public async Task<IntegrationDetails?> GetIntegrationDetails([Required] Guid id)
        {
            return await _integrationService.GetIntegrationDetailsAsync(id);
        }

        [HttpPost(Name = "AddIntegration")]
        public async Task<bool> AddIntegration([FromBody] AddIntegrationDto dto)
        {
            await _integrationService.AddIntegrationAsync(dto);
            return true;
        }

        [HttpPut("{id}", Name = "UpdateIntegration")]
        public async Task<bool> UpdateIntegration([Required] Guid id, [FromBody] UpdateIntegrationDto dto)
        {
            await _integrationService.UpdateIntegrationAsync(id, dto);
            return true;
        }

        [HttpDelete("{id}", Name = "DeleteIntegration")]
        public async Task<bool> DeleteIntegration([Required] Guid id)
        {
            await _integrationService.DeleteIntegrationAsync(id);
            return true;
        }

        [HttpGet("{id}/standing/days/{days}", Name = "GetIntegrationStandingByDays")]
        public async Task<Dictionary<DateOnly, decimal>> GetIntegrationStandingByDays([Required] Guid id, [Required] int days = 7)
        {
            return await _integrationService.GetIntegrationStandingByDaysAsync(id, days);
        }

        [HttpGet("{id}/measuring", Name = "GetMeasuringsByIntegration")]
        public async Task<List<DailyHoldingDto>> GetMeasuringsByIntegration([Required] Guid id)
        {
            return await _measuringService.GetMeasuringsByIntegrationAsync(id);
        }

        [HttpPost("{id}/measuring", Name = "AddIntegrationMeasuring")]
        public async Task<bool> AddIntegrationMeasuring([Required] Guid id, [FromBody] MeasuringService.AddMeasuringDto dto)
        {
            await _measuringService.AddIntegrationMeasuringAsync(id, dto);
            return true;
        }

        [HttpDelete("{id}/measuring", Name = "DeleteIntegrationMeasuring")]
        public async Task<bool> DeleteIntegrationMeasuring([Required] Guid id, [Required] string symbol, [Required] DateOnly date)
        {
            await _measuringService.DeleteMeasuringAsync(id, symbol, date);
            return true;
        }
    }
}
