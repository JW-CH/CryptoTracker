using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;
using ThreeFourteen.AlphaVantage;

public class AlphaVantageStockLogic : IStockLogic
{
    private readonly ILogger _logger;
    private readonly IFiatLogic _fiatLogic;
    private readonly AlphaVantage _client;

    public AlphaVantageStockLogic(ILogger logger, IFiatLogic fiatLogic, ICryptoTrackerConfig config)
    {
        _logger = logger;
        _fiatLogic = fiatLogic;
        _client = new AlphaVantage(config.StockApi);
    }

    public Task<IEnumerable<Stock>> GetAllStocksAsync()
    {
        throw new NotImplementedException("Alpha Vantage unterstützt keine vollständige Liste aller Aktien.");
    }

    public async Task<AssetMetadata> GetStockByIdAsync(string currency, string id)
    {
        var results = await GetStocksByIdsAsync(currency, new List<string> { id });
        return results.FirstOrDefault();
    }

    public async Task<List<AssetMetadata>> GetStocksByIdsAsync(string currency, List<string> ids)
    {
        var result = new List<AssetMetadata>();
        var fiatRates = new Dictionary<string, decimal>();

        foreach (var symbol in ids)
        {
            try
            {
                var timeSeries = await _client.Stocks.Daily(symbol)
                    .SetOutputSize(OutputSize.Compact)
                    .GetAsync();

                var latest = timeSeries.Data.ToList().FirstOrDefault();

                if (latest == null)
                {
                    _logger.LogWarning($"Keine Daten für {symbol} gefunden.");
                    continue;
                }

                decimal price = Convert.ToDecimal(latest.Close);
                _logger.LogTrace($"Aktueller Preis für {symbol}: {price}");

                if (!string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
                {
                    if (!fiatRates.ContainsKey("USD"))
                    {
                        var rate = await _fiatLogic.GetFiatByIdAsync("USD", currency);
                        fiatRates["USD"] = rate.Price;
                    }

                    price *= fiatRates["USD"];
                }

                result.Add(new AssetMetadata
                {
                    AssetId = symbol,
                    Symbol = symbol,
                    Name = symbol,
                    Price = price,
                    Currency = currency
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Abrufen der Daten für {symbol}");
            }

            // Alpha Vantage hat ein Limit von 5 Anfragen pro Minute
            await Task.Delay(15000);
        }

        return result;
    }
}
