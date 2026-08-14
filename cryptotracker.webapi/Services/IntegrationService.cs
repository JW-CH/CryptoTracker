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

            // One unfiltered query for today, aggregated per integration — consistent
            // with the dashboard (hidden assets excluded, forward-fill applied)
            var measurings = await _portfolioQueryService.GetAssetDayMeasuringAsync(_clock.Today);
            var valueByIntegration = measurings
                .SelectMany(m => m.IntegrationValues.Select(iv => (iv.Integration.Id, Value: iv.Amount * m.Price)))
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));

            return integrations.Select(integration =>
            {
                var dto = IntegrationDto.FromModel(integration);
                dto.LastSyncedAtUtc = lastSynced.TryGetValue(integration.Id, out var last) ? last : null;
                dto.CurrentValue = valueByIntegration.GetValueOrDefault(integration.Id, 0m);
                return dto;
            }).ToList();
        }

        public async Task<Dictionary<DateOnly, decimal>> GetIntegrationStandingByDaysAsync(Guid id, int days)
        {
            var today = _clock.Today;
            var dayList = new List<DateOnly>();
            for (int i = 0; i < days; i++)
            {
                dayList.Add(today.AddDays(-i));
            }

            var batchResult = await _portfolioQueryService.GetAssetDayMeasuringBatchAsync(dayList, integrationId: id);

            return batchResult
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value.Sum(m => m.TotalValue));
        }

        public async Task UpdateIntegrationAsync(Guid id, UpdateIntegrationDto dto)
        {
            var integration = await _db.ExchangeIntegrations.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new InvalidOperationException("Integration not found");

            if (!integration.IsManual && !string.Equals(integration.Name, dto.Name, StringComparison.Ordinal))
                throw new InvalidOperationException("Automatic integrations are matched by name from the configuration and cannot be renamed");

            if (await _db.ExchangeIntegrations.AnyAsync(x => x.Id != id && x.Name.ToLower() == dto.Name.ToLower()))
                throw new InvalidOperationException("Integration with this name already exists");

            integration.Name = dto.Name;
            integration.Description = dto.Description;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteIntegrationAsync(Guid id)
        {
            var integration = await _db.ExchangeIntegrations.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new InvalidOperationException("Integration not found");

            // DailyHoldings go with it via cascade delete
            _db.ExchangeIntegrations.Remove(integration);
            await _db.SaveChangesAsync();
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

        public struct UpdateIntegrationDto
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
