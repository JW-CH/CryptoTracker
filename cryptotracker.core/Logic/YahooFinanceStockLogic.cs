using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;
using YahooFinanceApi;

public class YahooFinanceStockLogic : IStockLogic
{
    private ILogger _logger;
    private IFiatLogic _fiatLogic;
    public YahooFinanceStockLogic(ILogger logger, IFiatLogic fiatLogic)
    {
        _logger = logger;
        _fiatLogic = fiatLogic;
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

        Dictionary<string, decimal> fiatPrices = new Dictionary<string, decimal>();

        foreach (var security in securities)
        {
            _logger.LogTrace($"GetStocksByIdsAsync: {security.Key} - {security.Value.RegularMarketPrice}");

            var price = Convert.ToDecimal(security.Value.RegularMarketPrice);

            if (security.Value.Currency.ToLower() != currency.ToLower())
            {
                if (!fiatPrices.ContainsKey(security.Value.Currency))
                {
                    var fiatMetaData = await _fiatLogic.GetFiatByIdAsync(security.Value.Currency, currency);
                    fiatPrices.Add(security.Value.Currency, fiatMetaData.Price);
                }
                price = price * fiatPrices[security.Value.Currency];
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