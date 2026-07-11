using cryptotracker.core.Interfaces;
using cryptotracker.webapi.Dtos;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Services
{
    public class PortfolioQueryService
    {
        private readonly DatabaseContext _db;
        private readonly ICryptoTrackerConfig _config;

        public PortfolioQueryService(DatabaseContext db, ICryptoTrackerConfig config)
        {
            _db = db;
            _config = config;
        }

        public async Task<List<AssetHoldingDto>> GetAssetDayMeasuringAsync(DateOnly day, string? symbol = null, Guid? integrationId = null)
        {
            var result = await GetAssetDayMeasuringBatchAsync([day], symbol, integrationId);
            return result.GetValueOrDefault(day) ?? [];
        }

        public async Task<Dictionary<DateOnly, List<AssetHoldingDto>>> GetAssetDayMeasuringBatchAsync(
            List<DateOnly> days, string? symbol = null, Guid? integrationId = null)
        {
            if (days.Count == 0)
                return new Dictionary<DateOnly, List<AssetHoldingDto>>();

            var maxFillDays = _config.MaxFillDays;

            var assets = _db.Assets.AsQueryable();
            if (symbol == null)
                assets = assets.Where(x => !x.IsHidden);
            else
                assets = assets.Where(x => x.Symbol.ToLower() == symbol.ToLower());

            var integrations = _db.ExchangeIntegrations.AsQueryable();
            if (integrationId.HasValue)
                integrations = integrations.Where(x => x.Id == integrationId);

            var assetList = await assets.AsNoTracking().ToListAsync();
            var integrationList = await integrations.AsNoTracking().ToListAsync();

            var allSymbols = assetList.Select(x => x.Symbol).ToList();
            var allIntegrationIds = integrationList.Select(x => x.Id).ToList();

            if (allSymbols.Count == 0 || allIntegrationIds.Count == 0)
                return days.ToDictionary(d => d, _ => new List<AssetHoldingDto>());

            var maxDay = days.Max();
            var currency = _config.BaseCurrency;

            var allPriceHistories = await _db.AssetPriceHistory
                .AsNoTracking()
                .Where(x => x.Date <= maxDay && x.Currency == currency)
                .Where(x => allSymbols.Contains(x.Symbol))
                .ToListAsync();

            var pricesBySymbol = allPriceHistories
                .GroupBy(x => x.Symbol)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Date).ToList());

            // holdings older than <oldest requested day> - maxFillDays can never be
            // carried forward into the requested range, so don't even load them
            var minDay = days.Min().AddDays(-maxFillDays);
            var allHoldings = await _db.DailyHoldings
                .AsNoTracking()
                .Include(x => x.Integration)
                .Where(x => x.Date <= maxDay && x.Date >= minDay)
                .Where(x => allSymbols.Contains(x.Symbol))
                .Where(x => allIntegrationIds.Contains(x.IntegrationId))
                .ToListAsync();

            var holdingsByKey = allHoldings
                .GroupBy(x => (x.Symbol, x.IntegrationId))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Date).ToList());

            var result = new Dictionary<DateOnly, List<AssetHoldingDto>>();
            foreach (var day in days)
            {
                result[day] = BuildDayResult(day, assetList, integrationList, pricesBySymbol, holdingsByKey, maxFillDays);
            }

            return result;
        }

        private static List<AssetHoldingDto> BuildDayResult(
            DateOnly day,
            List<Asset> assets,
            List<ExchangeIntegration> integrations,
            Dictionary<string, List<AssetPriceHistory>> pricesBySymbol,
            Dictionary<(string Symbol, Guid IntegrationId), List<DailyHolding>> holdingsByKey,
            int maxFillDays)
        {
            // forward-fill limit: only carry a holding into this day if it is at most
            // maxFillDays old; older data counts as missing instead of silently stale
            var minDay = day.AddDays(-maxFillDays);
            var result = new List<AssetHoldingDto>();

            foreach (var asset in assets)
            {
                decimal price = 0m;
                if (pricesBySymbol.TryGetValue(asset.Symbol, out var prices))
                {
                    var ph = prices.FirstOrDefault(x => x.Date <= day);
                    if (ph != null) price = ph.Price;
                }

                var holdings = new List<DailyHolding>();
                bool hasAnyData = false;

                foreach (var integration in integrations)
                {
                    if (!holdingsByKey.TryGetValue((asset.Symbol, integration.Id), out var groupHoldings))
                        continue;

                    // exactly one snapshot per day — the newest one at or before the
                    // requested day is the integration's holding for that day
                    var latest = groupHoldings.FirstOrDefault(x => x.Date <= day && x.Date >= minDay);
                    if (latest == null) continue;

                    hasAnyData = true;
                    holdings.Add(latest);
                }

                if (!hasAnyData) continue;

                var dto = AssetHoldingDto.SumFromModels(asset, holdings, price);

                // sold positions (explicit zero measurings) shouldn't show up as 0-rows;
                // the total is summed across integrations, so partial holdings survive
                if (dto.TotalAmount == 0) continue;

                result.Add(dto);
            }

            return result;
        }
    }
}
