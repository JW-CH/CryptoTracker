using cryptotracker.webapi.Dtos;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Services
{
    public class IntegrationService
    {
        private readonly DatabaseContext _db;
        private readonly PortfolioQueryService _portfolioQueryService;
        private readonly PortfolioClock _clock;

        public IntegrationService(DatabaseContext db, PortfolioQueryService portfolioQueryService, PortfolioClock clock)
        {
            _db = db;
            _portfolioQueryService = portfolioQueryService;
            _clock = clock;
        }

        public async Task<List<IntegrationDto>> GetIntegrationsAsync()
        {
            var integrations = await _db.ExchangeIntegrations.ToListAsync();

            var lastSynced = await _db.DailyHoldings
                .GroupBy(x => x.IntegrationId)
                .Select(g => new { g.Key, Last = g.Max(x => x.RecordedAtUtc) })
                .ToDictionaryAsync(x => x.Key, x => x.Last);

            return integrations.Select(integration =>
            {
                var dto = IntegrationDto.FromModel(integration);
                dto.LastSyncedAtUtc = lastSynced.TryGetValue(integration.Id, out var last) ? last : null;
                return dto;
            }).ToList();
        }

        public async Task<IntegrationDetails?> GetIntegrationDetailsAsync(Guid id)
        {
            var integration = await _db.ExchangeIntegrations.FirstOrDefaultAsync(x => x.Id == id);

            if (integration == null) return null;

            var today = _clock.Today;

            var measurings = await _portfolioQueryService.GetAssetDayMeasuringAsync(today, integrationId: integration.Id);

            var details = IntegrationDetails.FromIntegration(integration, measurings);
            details.Integration.LastSyncedAtUtc = await _db.DailyHoldings
                .Where(x => x.IntegrationId == id)
                .MaxAsync(x => (DateTime?)x.RecordedAtUtc);

            return details;
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
