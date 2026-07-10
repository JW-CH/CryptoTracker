using cryptotracker.webapi.Dtos;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Services
{
    public class MeasuringService
    {
        private readonly DatabaseContext _db;

        public MeasuringService(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<List<AssetMeasuringDto>> GetMeasuringsByIntegrationAsync(Guid integrationId)
        {
            return (await _db.AssetMeasurings.Where(x => x.IntegrationId == integrationId).ToListAsync()).Select(AssetMeasuringDto.FromModel).ToList();
        }

        public async Task AddIntegrationMeasuringAsync(Guid integrationId, AddMeasuringDto dto)
        {
            var integration = await _db.ExchangeIntegrations.FindAsync(integrationId) ?? throw new KeyNotFoundException("Integration not found");

            if (!integration.IsManual) throw new InvalidOperationException("Integration is not manual");

            var asset = await _db.Assets.FindAsync(dto.Symbol) ?? throw new KeyNotFoundException("Asset not found");

            var today = dto.Date.Date;
            var tomorrow = today.AddDays(1);

            AssetMeasuring? measuring = await _db.AssetMeasurings.FirstOrDefaultAsync(x => x.Symbol == dto.Symbol && x.IntegrationId == integration.Id && x.Timestamp >= today && x.Timestamp < tomorrow);

            if (measuring != null)
            {
                measuring.Timestamp = dto.Date;
                measuring.Amount = dto.Amount;
            }
            else
            {
                measuring = new AssetMeasuring()
                {
                    Symbol = asset.Symbol,
                    IntegrationId = integration.Id,
                    Timestamp = dto.Date,
                    Amount = dto.Amount
                };
                await _db.AssetMeasurings.AddAsync(measuring);
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteMeasuringAsync(Guid id)
        {
            var measuring = await _db.AssetMeasurings.Include(x => x.Integration).FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Measuring not found");

            if (!measuring.Integration.IsManual) throw new InvalidOperationException("Integration is not manual");

            _db.AssetMeasurings.Remove(measuring);

            await _db.SaveChangesAsync();
        }

        public struct AddMeasuringDto
        {
            public string Symbol { get; set; }
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
