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

        public IntegrationController(IntegrationService integrationService)
        {
            _integrationService = integrationService;
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
    }
}
