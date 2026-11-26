using System.Globalization;
using System.Numerics;
using System.Text.Json;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.database.Models;

namespace cryptotracker.core.Logic.Integrations;

public class EvmIntegrationProvider : IIntegrationProvider
{
    public static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);

    private static readonly Chain[] Chains =
    [
        new(CryptoTrackerIntegrationType.Ethereum, "https://eth.blockscout.com", "ETH"),
        new(CryptoTrackerIntegrationType.Base, "https://base.blockscout.com", "ETH"),
        new(CryptoTrackerIntegrationType.Arbitrum, "https://arbitrum.blockscout.com", "ETH"),
        new(CryptoTrackerIntegrationType.Optimism, "https://optimism.blockscout.com", "ETH"),
        new(CryptoTrackerIntegrationType.Polygon, "https://polygon.blockscout.com", "POL"),
        new(CryptoTrackerIntegrationType.Gnosis, "https://gnosis.blockscout.com", "XDAI"),
    ];

    private readonly Chain _chain;
    private readonly IHttpClientFactory _httpClientFactory;

    public EvmIntegrationProvider(CryptoTrackerIntegrationType type, IHttpClientFactory httpClientFactory)
        : this(Chains.Single(chain => chain.Type == type), httpClientFactory)
    {
    }

    public static IEnumerable<EvmIntegrationProvider> CreateAll(IHttpClientFactory httpClientFactory)
    {
        return Chains.Select(chain => new EvmIntegrationProvider(chain, httpClientFactory));
    }

    private EvmIntegrationProvider(Chain chain, IHttpClientFactory httpClientFactory)
    {
        _chain = chain;
        _httpClientFactory = httpClientFactory;
    }

    public CryptoTrackerIntegrationType Type => _chain.Type;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegrationSource source)
    {
        var address = source.Key?.Trim();
        if (string.IsNullOrEmpty(address))
        {
            throw new InvalidOperationException($"Missing address for {_chain.Type} integration");
        }

        using var client = CreateClient();
        var addressJson = await GetJsonAsync(client, $"{_chain.BlockscoutBaseUrl}/api/v2/addresses/{Uri.EscapeDataString(address)}");
        var native = ReadNativeBalance(addressJson);
        if (native == 0)
        {
            return [];
        }

        return
        [
            new BalanceResult
            {
                Symbol = _chain.NativeSymbol,
                Balance = native,
                AssetType = AssetType.Crypto,
            },
        ];
    }

    private decimal ReadNativeBalance(JsonElement addressJson)
    {
        if (!addressJson.TryGetProperty("coin_balance", out var coinBalance) || coinBalance.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Blockscout returned no native balance for {_chain.Type}");
        }

        var raw = coinBalance.ValueKind switch
        {
            JsonValueKind.String => coinBalance.GetString(),
            JsonValueKind.Number => coinBalance.GetRawText(),
            _ => null,
        };

        if (string.IsNullOrEmpty(raw))
        {
            throw new InvalidOperationException($"Blockscout returned no native balance for {_chain.Type}");
        }

        return ScaleWei(raw);
    }

    private async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to fetch {_chain.Type} balance from {url}: {response.StatusCode}");
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = HttpTimeout;
        client.DefaultRequestHeaders.UserAgent.ParseAdd("cryptotracker");
        return client;
    }

    private static decimal ScaleWei(string raw)
    {
        if (!BigInteger.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException($"Could not parse native balance '{raw}'");
        }

        if (value.Sign < 0)
        {
            throw new InvalidOperationException($"Negative native balance '{raw}'");
        }

        if (value.IsZero)
        {
            return 0;
        }

        var text = value.ToString(CultureInfo.InvariantCulture);
        if (text.Length <= 18)
        {
            text = text.PadLeft(19, '0');
        }

        var split = text.Length - 18;
        text = string.Concat(text.AsSpan(0, split), ".", text.AsSpan(split));
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var scaled))
        {
            throw new InvalidOperationException($"Native balance '{raw}' does not fit in a decimal");
        }

        return scaled;
    }

    private sealed record Chain(CryptoTrackerIntegrationType Type, string BlockscoutBaseUrl, string NativeSymbol);
}
