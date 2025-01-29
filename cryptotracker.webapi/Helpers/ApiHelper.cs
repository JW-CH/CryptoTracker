using cryptotracker.database.DTOs;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

public static class ApiHelper
{
    public static List<AssetMeasuringDto> GetAssetDayMeasuring(DatabaseContext db, DateTime day, string? symbol = null, Guid? integrationId = null)
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

        return GetAssetDayMeasuring(db, day, assets.ToList(), integrations.ToList());
    }

    public static List<AssetMeasuringDto> GetAssetDayMeasuring(DatabaseContext db, DateTime day, List<Asset> assets, List<ExchangeIntegration> integrations)
    {
        var result = new List<AssetMeasuringDto>();

        var allSymbols = assets.Select(x => x.Symbol).ToList();
        var allIntegrations = integrations.Select(x => x.Id).ToList();

        var currency = "chf";
        var priceHistories = db.AssetPriceHistory
            .Where(x => x.Date <= day.Date && x.Currency == currency)
            .Where(x => allSymbols.Contains(x.Symbol))
            .OrderByDescending(x => x.Date)
            .ToList();

        var assetMeasurings = db.AssetMeasurings
            .Include(x => x.Integration)
            .Where(x => x.Timestamp.Date <= day.Date)
            .Where(x => allSymbols.Contains(x.Symbol))
            .Where(x => allIntegrations.Contains(x.IntegrationId))
            .OrderByDescending(x => x.Timestamp)
            .ToList();

        foreach (var asset in assets.Where(x => assetMeasurings.Select(x => x.Symbol).Contains(x.Symbol)))
        {
            var priceHistory = priceHistories
                .FirstOrDefault(x => x.Symbol == asset.Symbol);

            var allMeasurings = new List<AssetMeasuring>();
            foreach (var integration in integrations)
            {
                var datt = assetMeasurings
                    .Where(x => x.IntegrationId == integration.Id && x.Symbol == asset.Symbol)
                    .FirstOrDefault()?.Timestamp.Date;

                if (!datt.HasValue) continue;

                var measurings = assetMeasurings
                    .Where(x => x.Timestamp.Date == datt && x.IntegrationId == integration.Id && x.Symbol == asset.Symbol)
                    .ToList();

                allMeasurings.AddRange(measurings);
            }

            var dto = AssetMeasuringDto.SumFromModels(asset, allMeasurings, priceHistory?.Price ?? 0m);

            result.Add(dto);
        }

        return result;
    }
}