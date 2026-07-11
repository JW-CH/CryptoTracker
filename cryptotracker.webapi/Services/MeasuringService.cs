using cryptotracker.webapi.Dtos;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Services
{
    public class MeasuringService
    {
        private readonly DatabaseContext _db;
        private readonly PortfolioClock _clock;

        public MeasuringService(DatabaseContext db, PortfolioClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public async Task<List<DailyHoldingDto>> GetMeasuringsByIntegrationAsync(Guid integrationId)
        {
            return (await _db.DailyHoldings.AsNoTracking().Where(x => x.IntegrationId == integrationId).ToListAsync()).Select(DailyHoldingDto.FromModel).ToList();
        }

        public async Task AddIntegrationMeasuringAsync(Guid integrationId, AddMeasuringDto dto)
        {
            var integration = await _db.ExchangeIntegrations.FindAsync(integrationId) ?? throw new KeyNotFoundException("Integration not found");

            if (!integration.IsManual) throw new InvalidOperationException("Integration is not manual");

            var asset = await _db.Assets.FindAsync(dto.Symbol) ?? throw new KeyNotFoundException("Asset not found");

            var holding = await _db.DailyHoldings.FindAsync(integration.Id, asset.Symbol, dto.Date);

            if (holding == null)
            {
                holding = new DailyHolding()
                {
                    IntegrationId = integration.Id,
                    Symbol = asset.Symbol,
                    Date = dto.Date,
                    Source = HoldingSource.Manual,
                };
                _db.DailyHoldings.Add(holding);
            }

            holding.Amount = dto.Amount;
            holding.RecordedAtUtc = _clock.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteMeasuringAsync(Guid integrationId, string symbol, DateOnly date)
        {
            var holding = await _db.DailyHoldings.Include(x => x.Integration)
                .FirstOrDefaultAsync(x => x.IntegrationId == integrationId && x.Symbol == symbol && x.Date == date)
                ?? throw new KeyNotFoundException("Measuring not found");

            if (!holding.Integration.IsManual) throw new InvalidOperationException("Integration is not manual");

            _db.DailyHoldings.Remove(holding);

            await _db.SaveChangesAsync();
        }

        public struct AddMeasuringDto
        {
            public string Symbol { get; set; }
            public DateOnly Date { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
