using System.Text.Json;
using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;

public class FiatLogic : IFiatLogic
{
    private ILogger _logger;
    public FiatLogic(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<AssetMetadata> GetFiatByIdAsync(string currency, string id)
    {
        var assetMetaDataResults = await GetFiatsByIdsAsync(currency, new List<string> { id });

        return assetMetaDataResults.FirstOrDefault();
    }

    public async Task<List<AssetMetadata>> GetFiatsByIdsAsync(string currency, List<string> ids)
    {
        ids = ids.Distinct().Select(x => x.ToLower()).ToList();

        var result = new List<AssetMetadata>();

        if (ids.Count == 0) return result;
        var fiatSymbols = string.Join(",", ids);

        var fiatList = await GetFiatList();

        if (ids.Contains(currency.ToLower()))
        {
            result.Add(new AssetMetadata()
            {
                AssetId = currency,
                Symbol = currency,
                Image = "",
                Currency = currency,
                Name = fiatList.FirstOrDefault(x => x.Symbol.ToLower() == currency.ToLower()).Name ?? currency,
                Price = 1
            });
        }

        if (fiatSymbols == currency.ToLower())
        {
            return result;
        }

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "cryptotracker");
        string apiUrl = $"https://api.frankfurter.app/latest?base={currency}&symbols={fiatSymbols}";
        var response = await client.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to fetch Fiat balances: {response.StatusCode}");
            _logger.LogError(await response.Content.ReadAsStringAsync());
            return result;
        }

        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        var ratesProperty = data.GetProperty("rates");

        var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(ratesProperty);

        if (rates == null)
        {
            _logger.LogError($"Failed to fetch Fiat balances: No balances were returned");
            return result;
        }

        foreach (var item in rates)
        {
            var id = item.Key;
            var name = fiatList.FirstOrDefault(x => x.Symbol.ToLower() == item.Key.ToLower()).Name ?? item.Key;
            var image = "";
            var symbol = item.Key;
            var price = item.Value;

            result.Add(new AssetMetadata()
            {
                AssetId = id,
                Symbol = symbol,
                Image = image,
                Currency = currency,
                Name = name,
                Price = price
            });
        }

        return result;
    }

    private List<Fiat>? _fiatList;
    public async Task<List<Fiat>> GetFiatList()
    {
        if (_fiatList != null) return _fiatList;

        var client = new HttpClient();
        var url = "https://api.frankfurter.app/currencies";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to fetch Fiat list: {response.StatusCode}");
            _logger.LogError(await response.Content.ReadAsStringAsync());
            return new();
        }

        var json = await response.Content.ReadAsStringAsync();
        var fiatDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        if (fiatDictionary == null)
        {
            _logger.LogError($"Failed to fetch Fiat list");
            return new();
        }

        _fiatList = fiatDictionary.Select(kvp => new Fiat { Symbol = kvp.Key, Name = kvp.Value }).ToList();

        return _fiatList;
    }
}