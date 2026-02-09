using System.Threading.Tasks;
using cryptotracker.database.DTOs;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

public static class ApiHelper
{
    public static async Task<List<MessungDto>> GetAssetDayMeasuring(DatabaseContext db, DateOnly day, string? symbol = null, Guid? integrationId = null)
    {
        var assets = db.Assets.AsQueryable();

        if (symbol == null)
        {
            assets = assets.Where(x => !x.IsHidden);
        }
        else
        {
            assets = assets.Where(x => x.Symbol.ToLower() == symbol.ToLower());
        }

        var integrations = db.ExchangeIntegrations.AsQueryable();
        if (integrationId.HasValue)
        {
            integrations = integrations.Where(x => x.Id == integrationId);
        }

        return await GetAssetDayMeasuringAsync(db, day, await assets.ToListAsync(), await integrations.ToListAsync());
    }

    public static async Task<List<MessungDto>> GetAssetDayMeasuringAsync(DatabaseContext db, DateOnly day, List<Asset> assets, List<ExchangeIntegration> integrations)
    {
        var result = new List<MessungDto>();

        var allSymbols = assets.Select(x => x.Symbol).ToList();
        var allIntegrations = integrations.Select(x => x.Id).ToList();

        var currency = "chf";
        var priceHistories = await db.AssetPriceHistory
            .Where(x => x.Date <= day && x.Currency == currency)
            .Where(x => allSymbols.Contains(x.Symbol))
            .GroupBy(x => x.Symbol)
            .Select(g => g.OrderByDescending(x => x.Date).First())
            .ToListAsync();

        var assetMeasurings = await db.AssetMeasurings
            .Include(x => x.Integration)
            .Where(x => x.Timestamp.Date <= day.ToDateTime(new TimeOnly(0, 0, 0), DateTimeKind.Utc))
            .Where(x => allSymbols.Contains(x.Symbol))
            .Where(x => allIntegrations.Contains(x.IntegrationId))
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();

        foreach (var asset in assets.Where(x => assetMeasurings.Select(x => x.Symbol).Contains(x.Symbol)))
        {
            var priceHistory = priceHistories
                .FirstOrDefault(x => x.Symbol == asset.Symbol);

            var allMeasurings = new List<AssetMeasuring>();
            foreach (var integration in integrations)
            {
                var today = assetMeasurings
                    .Where(x => x.IntegrationId == integration.Id && x.Symbol == asset.Symbol)
                    .FirstOrDefault()?.Timestamp.ToUniversalTime().Date;

                if (!today.HasValue) continue;
                var tomorrow = today.Value.AddDays(1);

                var measurings = assetMeasurings
                    .Where(x => x.Timestamp >= today && x.Timestamp < tomorrow && x.IntegrationId == integration.Id && x.Symbol == asset.Symbol)
                    .ToList();

                allMeasurings.AddRange(measurings);
            }

            var dto = MessungDto.SumFromModels(asset, allMeasurings, priceHistory?.Price ?? 0m);

            result.Add(dto);
        }

        return result;
    }
}