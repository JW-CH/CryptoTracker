using System.Text.Json;
using cryptotracker.core.Helpers;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using NBitcoin;

namespace cryptotracker.core.Logic.Integrations;

public class BitcoinIntegrationProvider : IIntegrationProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BitcoinIntegrationProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public CryptoTrackerIntegrationType Type => CryptoTrackerIntegrationType.Bitcoin;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegrationSource source)
    {
        var input = source.Key;

        if (input.StartsWith("xpub", StringComparison.OrdinalIgnoreCase) || input.StartsWith("zpub", StringComparison.OrdinalIgnoreCase))
        {
            string xpub = input;

            if (input.StartsWith("zpub", StringComparison.OrdinalIgnoreCase))
            {
                xpub = BitcoinHelper.ZpubToXpub(input);
            }

            ExtPubKey extPubKey = ExtPubKey.Parse(xpub, Network.Main);

            decimal totalBalance = 0;
            int i = 0;
            int transactions;
            do
            {
                KeyPath keyPath = new KeyPath($"0/{i}"); // Change path for receiving or change addresses
                PubKey pubKey = extPubKey.Derive(keyPath).PubKey;
                BitcoinAddress address = pubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);

                var res = await GetBitcoinAmountFromAddress(address.ToString());
                totalBalance += res.balance;
                transactions = res.transactions;

                i++;
            }
            while (transactions > 0);

            return new List<BalanceResult> { new BalanceResult { Symbol = "BTC", Balance = totalBalance, AssetType = AssetType.Crypto } };
        }
        else
        {
            var result = await GetBitcoinAmountFromAddress(input);
            return new List<BalanceResult> { new BalanceResult { Symbol = "BTC", Balance = result.balance, AssetType = AssetType.Crypto } };
        }
    }

    private async Task<(decimal balance, int transactions)> GetBitcoinAmountFromAddress(string address)
    {
        using var client = _httpClientFactory.CreateClient();

        string apiUrl = $"https://blockchain.info/balance?active={address}";
        HttpResponseMessage response = await client.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();

            var property = JsonSerializer.Deserialize<JsonElement>(json).GetProperty(address);

            var balance = property.GetProperty("final_balance").GetDecimal();
            var transactions = property.GetProperty("n_tx").GetInt32();

            return (BitcoinHelper.GetBitcoinFromSats(balance), transactions); // Convert satoshis to BTC
        }
        else
        {
            throw new InvalidOperationException($"Failed to fetch balance for address {address}: {response.StatusCode}");
        }
    }
}