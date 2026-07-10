using System.ComponentModel.DataAnnotations;
using cryptotracker.webapi.Dtos;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpDelete("{id}", Name = "DeleteMeasuringById")]
        public async Task<bool> DeleteMeasuringById([Required] Guid id)
        {
            await _measuringService.DeleteMeasuringAsync(id);
            return true;
        }
    }
}
