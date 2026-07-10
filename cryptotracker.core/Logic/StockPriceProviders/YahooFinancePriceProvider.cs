using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using cryptotracker.database.Models;
using Microsoft.Extensions.Logging;
using YahooFinanceApi;

namespace cryptotracker.core.Logic.StockPriceProviders;

public class YahooFinancePriceProvider : IPriceProvider
{
    private ILogger _logger;
    private IPriceProvider _currencyProvider;
    public YahooFinancePriceProvider(ILogger logger, IPriceProvider currencyProvider)
    {
        _logger = logger;
        _currencyProvider = currencyProvider;
    }

    public IEnumerable<AssetType> Handles => new[] { AssetType.Stock };

    public async Task<IEnumerable<ProviderAsset>> GetAssetsAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<AssetMetadata>> GetQuotesAsync(string baseCurrency, IEnumerable<string> externalIds)
    {
        externalIds = externalIds.Distinct().Select(x => x.ToLower()).ToList();

        _logger.LogTrace($"GetStocksByIdsAsync: {string.Join(",", externalIds)}");

        var result = new List<AssetMetadata>();

        if (externalIds.Count() == 0) return result;

        var securities = await Yahoo.Symbols(externalIds.ToArray())
        .Fields(Field.Symbol, Field.ShortName, Field.RegularMarketPrice, Field.Currency)
        .QueryAsync();

        Dictionary<string, decimal> currencyRates = new Dictionary<string, decimal>();

        foreach (var security in securities)
        {
            _logger.LogTrace($"GetStocksByIdsAsync: {security.Key} - {security.Value.RegularMarketPrice}");

            var price = Convert.ToDecimal(security.Value.RegularMarketPrice);

            if (security.Value.Currency.ToLower() != baseCurrency.ToLower())
            {
                if (!currencyRates.ContainsKey(security.Value.Currency))
                {
                    // value of 1 <stock currency> in <currency>
                    var rateMetadata = await _currencyProvider.GetQuotesAsync(baseCurrency, new[] { security.Value.Currency });
                    currencyRates.Add(security.Value.Currency, rateMetadata.First().Price);
                }
                price = price * currencyRates[security.Value.Currency];
            }

            var assetMetaData = new AssetMetadata
            {
                AssetId = security.Key,
                Name = security.Value.ShortName ?? security.Key,
                Price = price,
                Currency = baseCurrency,
                Symbol = security.Value.Symbol
            };

            result.Add(assetMetaData);
        }

        return result;
    }
}