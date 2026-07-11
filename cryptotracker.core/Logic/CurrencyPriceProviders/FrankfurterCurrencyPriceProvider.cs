using System.Text.Json;
using cryptotracker.core.Interfaces;
using cryptotracker.database.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace cryptotracker.core.Logic.CurrencyPriceProviders;

public class FrankfurterCurrencyPriceProvider : IPriceProvider
{
    private const string DefaultBaseUrl = "https://api.frankfurter.dev/v1";
    private const string CurrencyListCacheKey = "frankfurter:currencies";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;

    public FrankfurterCurrencyPriceProvider(ILogger logger, IHttpClientFactory httpClientFactory, IMemoryCache cache, string? baseUrl = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _baseUrl = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
    }

    public IEnumerable<AssetType> Handles => new[] { AssetType.Fiat };

    public async Task<IEnumerable<ProviderAsset>> GetAssetsAsync()
    {
        return await _cache.GetOrCreateAsync(CurrencyListCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            using var client = _httpClientFactory.CreateClient();
            var url = $"{_baseUrl}/currencies";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch currency list: {response.StatusCode}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var currencyDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (currencyDictionary == null)
            {
                throw new Exception("Failed to fetch currency list: no currencies were returned");
            }

            return currencyDictionary.Select(kvp => new ProviderAsset { Symbol = kvp.Key, Name = kvp.Value, ExternalId = kvp.Key }).ToList();
        }) ?? new List<ProviderAsset>();
    }

    public async Task<IEnumerable<AssetMetadata>> GetQuotesAsync(string baseCurrency, IEnumerable<string> externalIds)
    {
        _logger.LogTrace($"{nameof(GetQuotesAsync)}: {baseCurrency} - {string.Join(",", externalIds)}");

        var requestedIds = externalIds.Distinct().ToList();
        externalIds = requestedIds.Select(x => x.ToLower()).ToList();

        var result = new List<AssetMetadata>();

        if (externalIds.Count() == 0) return result;
        var externalIdsQuery = string.Join(",", externalIds);

        var currencyList = await GetAssetsAsync();

        if (externalIds.Contains(baseCurrency.ToLower()))
        {
            var requestedBase = requestedIds.First(x => x.ToLower() == baseCurrency.ToLower());
            result.Add(new AssetMetadata()
            {
                AssetId = requestedBase,
                Symbol = requestedBase,
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
            throw new Exception($"Failed to fetch currency rates: {response.StatusCode}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
        }

        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        var ratesProperty = data.GetProperty("rates");

        var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(ratesProperty);

        if (rates == null)
        {
            throw new Exception("Failed to fetch currency rates: no rates were returned");
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
