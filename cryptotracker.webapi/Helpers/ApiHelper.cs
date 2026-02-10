using cryptotracker.database.DTOs;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

public static class ApiHelper
{
    public static async Task<List<MessungDto>> GetAssetDayMeasuringAsync(DatabaseContext db, DateOnly day, string? symbol = null, Guid? integrationId = null)
    {
        var result = await GetAssetDayMeasuringBatchAsync(db, [day], symbol, integrationId);
        return result.GetValueOrDefault(day) ?? [];
    }

    public static async Task<Dictionary<DateOnly, List<MessungDto>>> GetAssetDayMeasuringBatchAsync(
        DatabaseContext db, List<DateOnly> days, string? symbol = null, Guid? integrationId = null)
    {
        if (days.Count == 0)
            return new Dictionary<DateOnly, List<MessungDto>>();

        var assets = db.Assets.AsQueryable();
        if (symbol == null)
            assets = assets.Where(x => !x.IsHidden);
        else
            assets = assets.Where(x => x.Symbol.ToLower() == symbol.ToLower());

        var integrations = db.ExchangeIntegrations.AsQueryable();
        if (integrationId.HasValue)
            integrations = integrations.Where(x => x.Id == integrationId);

        var assetList = await assets.ToListAsync();
        var integrationList = await integrations.ToListAsync();

        var allSymbols = assetList.Select(x => x.Symbol).ToList();
        var allIntegrationIds = integrationList.Select(x => x.Id).ToList();

        if (allSymbols.Count == 0 || allIntegrationIds.Count == 0)
            return days.ToDictionary(d => d, _ => new List<MessungDto>());

        var maxDay = days.Max();
        var currency = "chf";

        var allPriceHistories = await db.AssetPriceHistory
            .Where(x => x.Date <= maxDay && x.Currency == currency)
            .Where(x => allSymbols.Contains(x.Symbol))
            .ToListAsync();

        var pricesBySymbol = allPriceHistories
            .GroupBy(x => x.Symbol)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Date).ToList());

        var maxDayPlusOne = maxDay.AddDays(1).ToDateTime(new TimeOnly(0, 0, 0), DateTimeKind.Utc);
        var allMeasurings = await db.AssetMeasurings
            .Include(x => x.Integration)
            .Where(x => x.Timestamp < maxDayPlusOne)
            .Where(x => allSymbols.Contains(x.Symbol))
            .Where(x => allIntegrationIds.Contains(x.IntegrationId))
            .ToListAsync();

        var measuringsByKey = allMeasurings
            .GroupBy(x => (x.Symbol, x.IntegrationId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Timestamp).ToList());

        var result = new Dictionary<DateOnly, List<MessungDto>>();
        foreach (var day in days)
        {
            result[day] = BuildDayResult(day, assetList, integrationList, pricesBySymbol, measuringsByKey);
        }

        return result;
    }

    private static List<MessungDto> BuildDayResult(
        DateOnly day,
        List<Asset> assets,
        List<ExchangeIntegration> integrations,
        Dictionary<string, List<AssetPriceHistory>> pricesBySymbol,
        Dictionary<(string Symbol, Guid IntegrationId), List<AssetMeasuring>> measuringsByKey)
    {
        var dayPlusOne = day.AddDays(1).ToDateTime(new TimeOnly(0, 0, 0), DateTimeKind.Utc);
        var result = new List<MessungDto>();

        foreach (var asset in assets)
        {
            decimal price = 0m;
            if (pricesBySymbol.TryGetValue(asset.Symbol, out var prices))
            {
                var ph = prices.FirstOrDefault(x => x.Date <= day);
                if (ph != null) price = ph.Price;
            }

            var allMeasurings = new List<AssetMeasuring>();
            bool hasAnyData = false;

            foreach (var integration in integrations)
            {
                if (!measuringsByKey.TryGetValue((asset.Symbol, integration.Id), out var groupMeasurings))
                    continue;

                var latest = groupMeasurings.FirstOrDefault(x => x.Timestamp < dayPlusOne);
                if (latest == null) continue;

                hasAnyData = true;
                var latestDate = latest.Timestamp.ToUniversalTime().Date;
                var tomorrow = latestDate.AddDays(1);

                var measurings = groupMeasurings
                    .Where(x => x.Timestamp >= latestDate && x.Timestamp < tomorrow)
                    .ToList();

                allMeasurings.AddRange(measurings);
            }

            if (hasAnyData)
                result.Add(MessungDto.SumFromModels(asset, allMeasurings, price));
        }

        return result;
    }
}
