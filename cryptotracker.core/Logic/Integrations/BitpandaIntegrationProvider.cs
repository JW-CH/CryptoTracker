using System.Text.Json;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.core.Helpers;
using cryptotracker.database.Models;

namespace cryptotracker.core.Logic.Integrations;

public class BitpandaIntegrationProvider : IIntegrationProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BitpandaIntegrationProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public CryptoTrackerIntegrationType Type => CryptoTrackerIntegrationType.Bitpanda;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegrationSource source)
    {
        using var client = _httpClientFactory.CreateClient();
        client.UseApiKey(source.Secret);

        var result = new List<BalanceResult>();

        //var full = await GetBitpandaPortfolio(client);
        // result.AddRange(full.Select(x => new BalanceResult { Symbol = x.Attributes.AssetSymbol, Balance = Convert.ToDecimal(x.Attributes.AssetBalance) }).ToList());

        var accounts = await GetBitpandaAccounts(client);
        var fiat = await GetBitpandaFiatAccounts(client);

        result.AddRange(accounts.Select(account => new BalanceResult { Symbol = account.Attributes.CryptocoinSymbol, Balance = Convert.ToDecimal(account.Attributes.Balance), AssetType = AssetType.Crypto }).ToList());
        result.AddRange(fiat.Select(account => new BalanceResult { Symbol = account.Attributes.FiatSymbol, Balance = Convert.ToDecimal(account.Attributes.Balance), AssetType = AssetType.Fiat }).ToList());

        return result;
    }

    private async Task<List<Wallet>> GetBitpandaAccounts(HttpClient client)
    {
        var response = await client.GetAsync("https://api.bitpanda.com/v1/asset-wallets");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to fetch account balances for Bitpanda: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<BitpandaAssetWallet>(json);
        return list?.Data.Attributes.Cryptocoin.Attributes.Wallets.Where(x => Convert.ToDecimal(x.Attributes.Balance) > 0).ToList() ?? new();
    }

    private async Task<List<BitpandaFiatWallet>> GetBitpandaFiatAccounts(HttpClient client)
    {
        var response = await client.GetAsync("https://api.bitpanda.com/v1/fiatwallets");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to fetch fiat balances for Bitpanda: {response.StatusCode}{Environment.NewLine}{await response.Content.ReadAsStringAsync()}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<BitpandaFiatWalletResult>(json);
        return list?.Data.Where(x => Convert.ToDecimal(x.Attributes.Balance) > 0).ToList() ?? new();
    }

    private async Task<List<Portfolio>> GetBitpandaPortfolio(HttpClient client)
    {
        var response = await client.GetAsync("https://api.bitpanda.com/v2/portfolio/overview");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to fetch balances for Bitpanda: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var portfolio = JsonSerializer.Deserialize<BitpandaPortfolio>(json);
        return portfolio?.Data.Attributes.Portfolios ?? new();
    }
}
