using System.ComponentModel.DataAnnotations;
using cryptotracker.database.DTOs;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static cryptotracker.webapi.Services.MeasuringService;

namespace cryptotracker.webapi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MeasuringController : ControllerBase
    {
        private readonly MeasuringService _measuringService;

        public MeasuringController(MeasuringService measuringService)
        {
            _measuringService = measuringService;
        }

        [HttpGet("{id}", Name = "GetMeasuringsByIntegration")]
        public async Task<List<AssetMeasuringDto>> GetMeasuringsByIntegration([Required] Guid id)
        {
            return await _measuringService.GetMeasuringsByIntegrationAsync(id);
        }

        [HttpPost(Name = "AddIntegrationMeasuring")]
        public async Task<bool> AddIntegrationMeasuring([Required] Guid id, [FromBody] AddMeasuringDto dto)
        {
            await _measuringService.AddIntegrationMeasuringAsync(id, dto);
            return true;
        }

        [HttpDelete("{id}", Name = "DeleteMeasuringById")]
        public async Task<bool> DeleteMeasuringById([Required] Guid id)
        {
            await _measuringService.DeleteMeasuringAsync(id);
            return true;
        }
    }
}
