using System.Text.Json;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.database.Models;

namespace cryptotracker.core.Logic.Integrations;

public class EthereumIntegrationProvider : IIntegrationProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EthereumIntegrationProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public CryptoTrackerIntegrationType Type => CryptoTrackerIntegrationType.Ethereum;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegration integration)
    {
        var address = integration.Key;

        using var client = _httpClientFactory.CreateClient();

        string apiUrl = $"https://api.ethplorer.io/getAddressInfo/{address}?apiKey=freekey";
        HttpResponseMessage response = await client.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to fetch balance for address {address}: {response.StatusCode}");
        }

        string json = await response.Content.ReadAsStringAsync();

        var property = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("ETH");

        var balance = property.GetProperty("balance").GetDecimal();

        return new List<BalanceResult> { new BalanceResult { Symbol = "ETH", Balance = balance, AssetType = AssetType.Crypto } };
    }
}
