using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;
using YahooFinanceApi;

public class YahooFinanceStockLogic : IStockLogic
{
    private ILogger _logger;
    public YahooFinanceStockLogic(ILogger logger)
    {
        _logger = logger;
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

        foreach (var security in securities)
        {
            _logger.LogTrace($"GetStocksByIdsAsync: {security.Key} - {security.Value.RegularMarketPrice}");

            var assetMetaData = new AssetMetadata
            {
                AssetId = security.Key,
                Name = security.Value.ShortName ?? security.Key,
                Price = Convert.ToDecimal(security.Value.RegularMarketPrice),
                Currency = security.Value.Currency,
                Symbol = security.Value.Symbol
            };

            result.Add(assetMetaData);
        }

        _logger.LogTrace($"Finished GetStocksByIdsAsync");

        return result;
    }
}