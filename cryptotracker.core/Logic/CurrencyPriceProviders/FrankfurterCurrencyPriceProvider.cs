using System.Text.Json;
using cryptotracker.core.Interfaces;
using cryptotracker.database.Models;
using Microsoft.Extensions.Logging;

namespace cryptotracker.core.Logic.CurrencyPriceProviders;

public class FrankfurterCurrencyPriceProvider : IPriceProvider
{
    private const string DefaultBaseUrl = "https://api.frankfurter.dev/v1";

    private ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;

    public FrankfurterCurrencyPriceProvider(ILogger logger, IHttpClientFactory httpClientFactory, string? baseUrl = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _baseUrl = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
    }

    public IEnumerable<AssetType> Handles => new[] { AssetType.Fiat };

    private List<ProviderAsset>? _currencyList;

    public async Task<IEnumerable<ProviderAsset>> GetAssetsAsync()
    {
        if (_currencyList != null) return _currencyList;

        using var client = _httpClientFactory.CreateClient();
        var url = $"{_baseUrl}/currencies";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to fetch currency list: {response.StatusCode}");
            _logger.LogError(await response.Content.ReadAsStringAsync());
            return new List<ProviderAsset>();
        }

        var json = await response.Content.ReadAsStringAsync();
        var currencyDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        if (currencyDictionary == null)
        {
            _logger.LogError($"Failed to fetch currency list");
            return new List<ProviderAsset>();
        }

        _currencyList = currencyDictionary.Select(kvp => new ProviderAsset { Symbol = kvp.Key, Name = kvp.Value }).ToList();

        return _currencyList;
    }

    public async Task<IEnumerable<AssetMetadata>> GetQuotesAsync(string baseCurrency, IEnumerable<string> externalIds)
    {
        _logger.LogTrace($"{nameof(GetQuotesAsync)}: {baseCurrency} - {string.Join(",", externalIds)}");

        externalIds = externalIds.Distinct().Select(x => x.ToLower()).ToList();

        var result = new List<AssetMetadata>();

        if (externalIds.Count() == 0) return result;
        var externalIdsQuery = string.Join(",", externalIds);

        var currencyList = await GetAssetsAsync();

        if (externalIds.Contains(baseCurrency.ToLower()))
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

        if (externalIdsQuery == baseCurrency.ToLower())
        {
            return result;
        }

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "cryptotracker");
        string apiUrl = $"{_baseUrl}/latest?base={baseCurrency}&symbols={externalIdsQuery}";
        var response = await client.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"{nameof(GetQuotesAsync)}: Failed to fetch currency rates: {response.StatusCode}");
            _logger.LogError(await response.Content.ReadAsStringAsync());
            return result;
        }

        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        var ratesProperty = data.GetProperty("rates");

        var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(ratesProperty);

        if (rates == null)
        {
            _logger.LogError($"{nameof(GetQuotesAsync)}: Failed to fetch currency rates: No rates were returned");
            return result;
        }

        foreach (var item in rates)
        {
            _logger.LogTrace($"{nameof(GetQuotesAsync)}: {item.Key} - {item.Value}");

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