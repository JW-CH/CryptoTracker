using System.Text.Json;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.database.Models;

namespace cryptotracker.core.Logic.Integrations;

public class RippleIntegrationProvider : IIntegrationProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RippleIntegrationProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public CryptoTrackerIntegrationType Type => CryptoTrackerIntegrationType.Ripple;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegrationSource source)
    {
        var address = source.Key;

        using var client = _httpClientFactory.CreateClient();

        var apiUrl = $"https://api.xrpscan.com/api/v1/account/{address}";
        HttpResponseMessage response = await client.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to fetch balance for address {address}: {response.StatusCode}");
        }

        string json = await response.Content.ReadAsStringAsync();

        var balance = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("xrpBalance").GetString();

        decimal.TryParse(balance, out decimal result);

        return new List<BalanceResult> { new BalanceResult { Symbol = "XRP", Balance = result, AssetType = AssetType.Crypto } };
    }
}
