using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;
using TwelveDataSharp;
using TwelveDataSharp.Library.ResponseModels;

public class TwelveDataStockLogic : IStockLogic
{
    private readonly ILogger _logger;
    private readonly IFiatLogic _fiatLogic;
    private readonly TwelveDataClient _client;

    public TwelveDataStockLogic(ILogger logger, IFiatLogic fiatLogic, ICryptotrackerConfig config)
    {
        _logger = logger;
        _fiatLogic = fiatLogic;
        var httpClient = new HttpClient();
        _client = new TwelveDataClient(config.StockApi ?? "", httpClient);
    }

    public Task<IEnumerable<Stock>> GetAllStocksAsync()
    {
        // Nicht unterstützt in Free Tier – ggf. durch Konfig oder statische Liste lösen
        throw new NotImplementedException();
    }

    public async Task<AssetMetadata> GetStockByIdAsync(string currency, string id)
    {
        var results = await GetStocksByIdsAsync(currency, new List<string> { id });
        return results.FirstOrDefault();
    }

    public async Task<List<AssetMetadata>> GetStocksByIdsAsync(string currency, List<string> ids)
    {
        ids = ids.Distinct().ToList();

        _logger.LogTrace($"GetStocksByIdsAsync: {string.Join(",", ids)}");

        var result = new List<AssetMetadata>();
        var fiatPrices = new Dictionary<string, decimal>();

        foreach (var id in ids)
        {
            TwelveDataQuote quote;

            try
            {
                quote = await _client.GetQuoteAsync(id);

            }
            catch (Exception ex)
            {
                _logger.LogWarning($"TwelveData API call failed for {id}: {ex.Message}");
                continue;
            }

            if (quote == null || string.IsNullOrEmpty(quote.Symbol))
            {
                _logger.LogWarning($"No data returned for symbol {id}");
                continue;
            }

            decimal price = Convert.ToDecimal(quote.Close);
            _logger.LogTrace($"Current price for {id}: {price}");

            if (!string.Equals(quote.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                if (!fiatPrices.TryGetValue(quote.Currency, out var rate))
                {
                    var fiatMeta = await _fiatLogic.GetFiatByIdAsync(quote.Currency, currency);
                    fiatPrices[quote.Currency] = fiatMeta.Price;
                    rate = fiatMeta.Price;
                }

                price *= rate;
            }

            var assetMetaData = new AssetMetadata
            {
                AssetId = quote.Symbol,
                Name = quote.Name ?? quote.Symbol,
                Price = price,
                Currency = currency,
                Symbol = quote.Symbol
            };

            result.Add(assetMetaData);
        }

        return result;
    }
}
