using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FinnhubStockLogic : IStockLogic
{
    private readonly ILogger _logger;
    private readonly IFiatLogic _fiatLogic;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public FinnhubStockLogic(ILogger logger, IFiatLogic fiatLogic, ICryptotrackerConfig config)
    {
        _logger = logger;
        _fiatLogic = fiatLogic;
        _apiKey = config.StockApi ?? "";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://finnhub.io/api/v1/")
        };
    }

    public Task<IEnumerable<Stock>> GetAllStocksAsync()
    {
        throw new NotImplementedException("Finnhub bietet keine vollständige Liste aller Aktien.");
    }

    public async Task<AssetMetadata> GetStockByIdAsync(string currency, string id)
    {
        var result = await GetStocksByIdsAsync(currency, new List<string> { id });
        return result.FirstOrDefault();
    }

    public async Task<List<AssetMetadata>> GetStocksByIdsAsync(string currency, List<string> ids)
    {
        var results = new List<AssetMetadata>();
        var fiatRates = new Dictionary<string, decimal>();

        foreach (var symbol in ids)
        {
            try
            {
                var response = await _httpClient.GetAsync($"quote?symbol={symbol}&token={_apiKey}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<FinnhubQuote>();

                if (json == null || json.Current == 0)
                {
                    _logger.LogWarning($"Keine gültigen Daten für Symbol: {symbol}");
                    continue;
                }

                decimal price = json.Current;
                _logger.LogTrace($"Aktueller Preis für {symbol}: {price}");

                // Währung wird nicht direkt geliefert, Finnhub liefert in USD (für US-Märkte)
                var sourceCurrency = "USD";

                if (!string.Equals(currency, sourceCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    if (!fiatRates.ContainsKey(sourceCurrency))
                    {
                        var rate = await _fiatLogic.GetFiatByIdAsync(sourceCurrency, currency);
                        fiatRates[sourceCurrency] = rate.Price;
                    }

                    price *= fiatRates[sourceCurrency];
                }

                results.Add(new AssetMetadata
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
        }

        return results;
    }

    public class FinnhubQuote
    {
        [JsonPropertyName("c")]
        public decimal Current { get; set; }

        [JsonPropertyName("h")]
        public decimal High { get; set; }

        [JsonPropertyName("l")]
        public decimal Low { get; set; }

        [JsonPropertyName("o")]
        public decimal Open { get; set; }

        [JsonPropertyName("pc")]
        public decimal PreviousClose { get; set; }

        [JsonPropertyName("t")]
        public long Timestamp { get; set; }
    }
}
