using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;
using YahooFinanceApi;

public class YahooFinanceStockLogic : IStockLogic
{
    private ILogger _logger;
    private ICurrencyProvider _currencyProvider;
    public YahooFinanceStockLogic(ILogger logger, ICurrencyProvider currencyProvider)
    {
        _logger = logger;
        _currencyProvider = currencyProvider;
    }

    public Task<IEnumerable<Stock>> GetAllStocksAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<AssetMetadata> GetStockByIdAsync(string currency, string id)
    {
        var assetMetaDataResults = await GetStocksByIdsAsync(currency, new List<string> { id });

        return assetMetaDataResults.FirstOrDefault();
    }

    public async Task<List<AssetMetadata>> GetStocksByIdsAsync(string currency, List<string> ids)
    {
        ids = ids.Distinct().Select(x => x.ToLower()).ToList();

        _logger.LogTrace($"GetStocksByIdsAsync: {string.Join(",", ids)}");

        var result = new List<AssetMetadata>();

        if (ids.Count == 0) return result;

        var securities = await Yahoo.Symbols(ids.ToArray())
        .Fields(Field.Symbol, Field.ShortName, Field.RegularMarketPrice, Field.Currency)
        .QueryAsync();

        Dictionary<string, decimal> currencyRates = new Dictionary<string, decimal>();

        foreach (var security in securities)
        {
            _logger.LogTrace($"GetStocksByIdsAsync: {security.Key} - {security.Value.RegularMarketPrice}");

            var price = Convert.ToDecimal(security.Value.RegularMarketPrice);

            if (security.Value.Currency.ToLower() != currency.ToLower())
            {
                if (!currencyRates.ContainsKey(security.Value.Currency))
                {
                    // value of 1 <stock currency> in <currency>
                    var rateMetadata = await _currencyProvider.GetLatestRateAsync(currency, security.Value.Currency);
                    currencyRates.Add(security.Value.Currency, rateMetadata.Price);
                }
                price = price * currencyRates[security.Value.Currency];
            }

            var assetMetaData = new AssetMetadata
            {
                AssetId = security.Key,
                Name = security.Value.ShortName ?? security.Key,
                Price = price,
                Currency = currency,
                Symbol = security.Value.Symbol
            };

            result.Add(assetMetaData);
        }

        return result;
    }
}