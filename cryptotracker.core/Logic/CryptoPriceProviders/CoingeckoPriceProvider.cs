using System.Text.Json;
using cryptotracker.core.Interfaces;
using cryptotracker.database.Models;
using Microsoft.Extensions.Caching.Memory;

namespace cryptotracker.core.Logic.CryptoPriceProviders;

public class CoingeckoPriceProvider : IPriceProvider
{
    private const string CoinListCacheKey = "coingecko:coins";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public CoingeckoPriceProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public IEnumerable<AssetType> Handles => new[] { AssetType.Crypto };

    public async Task<IEnumerable<ProviderAsset>> GetAssetsAsync()
    {
        // failures throw, so only successful responses end up in the cache
        return await _cache.GetOrCreateAsync(CoinListCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            using var client = CreateClient();
            var url = "https://api.coingecko.com/api/v3/coins/list";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch Coin list: {response.StatusCode}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
            }

            var data = JsonSerializer.Deserialize<List<JsonElement>>(await response.Content.ReadAsStringAsync());

            if (data == null)
            {
                throw new Exception($"Failed to fetch Coin list: No coins were returned");
            }

            return data.Select(x => new ProviderAsset
            {
                ExternalId = x.GetProperty("id").GetString() ?? "",
                Name = x.GetProperty("name").GetString() ?? "",
                Symbol = x.GetProperty("symbol").GetString() ?? ""
            }).ToList();
        }) ?? new List<ProviderAsset>();
    }

    public async Task<IEnumerable<AssetMetadata>> GetQuotesAsync(string baseCurrency, IEnumerable<string> externalIds)
    {
        var result = new List<AssetMetadata>();

        using var client = CreateClient();
        string apiUrl = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency={baseCurrency}&ids={string.Join(",", externalIds)}";

        var response = await client.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch Coin balances: {response.StatusCode}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
        }

        var data = JsonSerializer.Deserialize<List<JsonElement>>(await response.Content.ReadAsStringAsync());

        if (data == null)
        {
            throw new Exception($"Failed to fetch Coin balances: No balances were returned");
        }

        foreach (var item in data)
        {
            var id = item.GetProperty("id").GetString() ?? "";
            var name = item.GetProperty("name").GetString() ?? "";
            var image = item.GetProperty("image").GetString() ?? "";
            var symbol = item.GetProperty("symbol").GetString() ?? "";
            var price = item.GetProperty("current_price").GetDecimal();

            result.Add(new AssetMetadata()
            {
                AssetId = id,
                Symbol = symbol,
                Image = image,
                Currency = baseCurrency,
                Name = name,
                Price = price
            });

        }

        return result;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "cryptotracker");
        return client;
    }
}
