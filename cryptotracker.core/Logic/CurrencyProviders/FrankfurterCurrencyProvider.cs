using System.Text.Json;
using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;

public class FrankfurterCurrencyProvider : ICurrencyProvider
{
    private const string DefaultBaseUrl = "https://api.frankfurter.dev/v1";

    private ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;

    public FrankfurterCurrencyProvider(ILogger logger, IHttpClientFactory httpClientFactory, string? baseUrl = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _baseUrl = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
    }


    private List<Currency>? _currencyList;
    public async Task<IEnumerable<Currency>> GetCurrenciesAsync()
    {
        if (_currencyList != null) return _currencyList;

        using var client = _httpClientFactory.CreateClient();
        var url = $"{_baseUrl}/currencies";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to fetch currency list: {response.StatusCode}");
            _logger.LogError(await response.Content.ReadAsStringAsync());
            return new List<Currency>();
        }

        var json = await response.Content.ReadAsStringAsync();
        var currencyDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        if (currencyDictionary == null)
        {
            _logger.LogError($"Failed to fetch currency list");
            return new List<Currency>();
        }

        _currencyList = currencyDictionary.Select(kvp => new Currency { Symbol = kvp.Key, Name = kvp.Value }).ToList();

        return _currencyList;
    }


    public async Task<AssetMetadata> GetLatestRateAsync(string baseCurrency, string currency)
    {
        var assetMetaDataResults = await GetLatestRatesAsync(baseCurrency, new List<string> { currency });

        return assetMetaDataResults.FirstOrDefault();
    }

    public async Task<IEnumerable<AssetMetadata>> GetLatestRatesAsync(string baseCurrency, IEnumerable<string> symbols)
    {
        _logger.LogTrace($"{nameof(GetLatestRatesAsync)}: {baseCurrency} - {string.Join(",", symbols)}");

        symbols = symbols.Distinct().Select(x => x.ToLower()).ToList();

        var result = new List<AssetMetadata>();

        if (symbols.Count() == 0) return result;
        var symbolsQuery = string.Join(",", symbols);

        var currencyList = await GetCurrenciesAsync();

        if (symbols.Contains(baseCurrency.ToLower()))
        {
            result.Add(new AssetMetadata()
            {
                AssetId = baseCurrency,
                Symbol = baseCurrency,
                Image = "",
                Currency = baseCurrency,
                Name = currencyList.FirstOrDefault(x => x.Symbol.ToLower() == baseCurrency.ToLower()).Name ?? baseCurrency,
                Price = 1
            });
        }

        if (symbolsQuery == baseCurrency.ToLower())
        {
            return result;
        }

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "cryptotracker");
        string apiUrl = $"{_baseUrl}/latest?base={baseCurrency}&symbols={symbolsQuery}";
        var response = await client.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"{nameof(GetLatestRatesAsync)}: Failed to fetch currency rates: {response.StatusCode}");
            _logger.LogError(await response.Content.ReadAsStringAsync());
            return result;
        }

        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        var ratesProperty = data.GetProperty("rates");

        var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(ratesProperty);

        if (rates == null)
        {
            _logger.LogError($"{nameof(GetLatestRatesAsync)}: Failed to fetch currency rates: No rates were returned");
            return result;
        }

        foreach (var item in rates)
        {
            _logger.LogTrace($"{nameof(GetLatestRatesAsync)}: {item.Key} - {item.Value}");

            if (item.Value <= 0)
            {
                _logger.LogWarning($"Skipping currency {item.Key}: invalid rate {item.Value}");
                continue;
            }

            var id = item.Key;
            var name = currencyList.FirstOrDefault(x => x.Symbol.ToLower() == item.Key.ToLower()).Name ?? item.Key;
            var image = "";
            var symbol = item.Key;
            var price = item.Value;

            result.Add(new AssetMetadata()
            {
                AssetId = id,
                Symbol = symbol,
                Image = image,
                Currency = baseCurrency,
                Name = name,
                // Price is the amount of baseCurrency needed to buy 1 unit of the currency
                Price = 1m / price
            });
        }

        return result;
    }
}