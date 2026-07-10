using cryptotracker.webapi.Dtos;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Services
{
    public class IntegrationService
    {
        private readonly DatabaseContext _db;
        private readonly PortfolioQueryService _portfolioQueryService;

        public IntegrationService(DatabaseContext db, PortfolioQueryService portfolioQueryService)
        {
            _db = db;
            _portfolioQueryService = portfolioQueryService;
        }

        public async Task<List<IntegrationDto>> GetIntegrationsAsync()
        {
            return (await _db.ExchangeIntegrations.ToListAsync()).Select(IntegrationDto.FromModel).ToList();
        }

        public async Task<IntegrationDetails?> GetIntegrationDetailsAsync(Guid id)
        {
            var integration = await _db.ExchangeIntegrations.FirstOrDefaultAsync(x => x.Id == id);

            if (integration == null) return null;

            var today = DateOnly.FromDateTime(DateTime.Now);

            var measurings = await _portfolioQueryService.GetAssetDayMeasuringAsync(today, integrationId: integration.Id);

            return IntegrationDetails.FromIntegration(integration, measurings);
        }

        public async Task AddIntegrationAsync(AddIntegrationDto dto)
        {
            if (await _db.ExchangeIntegrations.AnyAsync(x => x.Name.ToLower() == dto.Name.ToLower())) throw new InvalidOperationException("Integration with this name already exists");

            var integration = new ExchangeIntegration
            {
                Name = dto.Name,
                Description = dto.Description,
                IsHidden = false,
                IsManual = true,
            };

            await _db.AddAsync(integration);
            await _db.SaveChangesAsync();
        }

        public struct AddIntegrationDto
        {
            public string Name { get; set; }
            public string? Description { get; set; }
        }

        public struct IntegrationDetails
        {
            public required IntegrationDto Integration { get; set; }
            public required List<AssetHoldingDto> Measurings { get; set; }

            public static IntegrationDetails FromIntegration(ExchangeIntegration integration, List<AssetHoldingDto> measurings)
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
