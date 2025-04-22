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

    public async Task<AssetMetadata> GetFiatByIdAsync(string baseCurrency, string currency)
    {
        var assetMetaDataResults = await GetFiatsByIdsAsync(baseCurrency, new List<string> { currency });

        return assetMetaDataResults.FirstOrDefault();
    }

    public async Task<List<AssetMetadata>> GetFiatsByIdsAsync(string baseCurrency, List<string> currencies)
    {
        _logger.LogTrace($"GetFiatsByIdsAsync: {baseCurrency} - {string.Join(",", currencies)}");

        currencies = currencies.Distinct().Select(x => x.ToLower()).ToList();

        var result = new List<AssetMetadata>();

        if (currencies.Count == 0) return result;
        var fiatSymbols = string.Join(",", currencies);

        var fiatList = await GetFiatList();

        if (currencies.Contains(baseCurrency.ToLower()))
        {
            result.Add(new AssetMetadata()
            {
                AssetId = baseCurrency,
                Symbol = baseCurrency,
                Image = "",
                Currency = baseCurrency,
                Name = fiatList.FirstOrDefault(x => x.Symbol.ToLower() == baseCurrency.ToLower()).Name ?? baseCurrency,
                Price = 1
            });
        }

        if (fiatSymbols == baseCurrency.ToLower())
        {
            return result;
        }

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "cryptotracker");
        string apiUrl = $"https://api.frankfurter.app/latest?base={baseCurrency}&symbols={fiatSymbols}";
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
            _logger.LogTrace($"GetFiatsByIdsAsync: {item.Key} - {item.Value}");

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
                Currency = baseCurrency,
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