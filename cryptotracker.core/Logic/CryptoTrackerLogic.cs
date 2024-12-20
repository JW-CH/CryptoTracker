using Coinbase.Net.Clients;
using Coinbase.Net.Interfaces.Clients;
using Coinbase.Net.Objects.Models;
using CryptoCom.Net.Clients;
using CryptoCom.Net.Interfaces.Clients;
using CryptoCom.Net.Objects.Models;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using cryptotracker.core.Helpers;
using cryptotracker.core.Models;
using ImmichFrame.Core.Helpers;
using NBitcoin;
using System.Text.Json;

namespace cryptotracker.core.Logic
{
    public static class CryptoTrackerLogic
    {
        public async static Task<IEnumerable<BalanceResult>> GetAvailableIntegrationBalances(CryptotrackerIntegration integration)
        {
            switch (integration.Type.ToLower())
            {
                case "bitpanda":
                    using (var client = new HttpClient())
                    {
                        client.UseApiKey(integration.Secret);
                        var accounts = await GetBitpandaAccounts(client);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Attributes.CryptocoinSymbol, Balance = Convert.ToDecimal(account.Attributes.Balance) }).ToList();
                    }
                case "cryptocom":
                    using (var cbClient = new CryptoComRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                    }))
                    {
                        var accounts = await GetCoinbaseAvailableAccounts(cbClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Quantity }).ToList();
                    }
                case "coinbase":
                    using (var cbClient = new CoinbaseRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                    }))
                    {
                        var accounts = await GetCoinbaseAvailableAccounts(cbClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.AvailableBalance.Value + account.HoldBalance.Value }).ToList();
                    }
                case "bitcoin":
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "BTC",
                            Balance = await GetBitcoinAvailableBalances(client, integration.Key)
                        }};
                    }

                default:
                    throw new NotImplementedException($"Integration {integration.Type} was not implemented!");
            }
        }

        /// <summary>
        /// Retrieves the available Bitcoin balances for a given input, which can be either an address or an extended public key (xpub).
        /// </summary>
        /// <param name="client">The HttpClient used to make the request.</param>
        /// <param name="input">The Bitcoin address or extended public key (xpub) to retrieve the balance for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the available balance in BTC.</returns>
        private async static Task<decimal> GetBitcoinAvailableBalances(HttpClient client, string input)
        {
            async Task<(decimal balance, int transactions)> GetBitcoinAmountFromAddress(HttpClient client, string address)
            {
                string apiUrl = $"https://blockchain.info/balance?active={address}";
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    var property = JsonSerializer.Deserialize<JsonElement>(json).GetProperty(address.ToString());

                    var balance = property.GetProperty("final_balance").GetDecimal();
                    var transactions = property.GetProperty("n_tx").GetInt32();

                    return (BitcoinHelper.GetBitcoinFromSats(balance), transactions); // Convert satoshis to BTC
                }
                else
                {
                    Console.WriteLine($"Failed to fetch balance for address {address}: {response.StatusCode}");
                    return (0, 0);
                }
            }

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
                int transactions = 0;
                do
                {
                    KeyPath keyPath = new KeyPath($"0/{i}"); // Change path for receiving or change addresses
                    PubKey pubKey = extPubKey.Derive(keyPath).PubKey;
                    BitcoinAddress address = pubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);

                    var res = await GetBitcoinAmountFromAddress(client, address.ToString());
                    totalBalance += res.balance;
                    transactions = res.transactions;

                    i++;
                }
                while (transactions > 0);

                return totalBalance;
            }
            else
            {
                return (await GetBitcoinAmountFromAddress(client, input)).balance;
            }
        }
        private async static Task<IEnumerable<CryptoComBalance>> GetCoinbaseAvailableAccounts(ICryptoComRestClient client)
        {
            WebCallResult<IEnumerable<CryptoComBalances>>? result = null;
            List<CryptoComBalance> accounts = new();

            result = await client.ExchangeApi.Account.GetBalancesAsync();

            if (!result.Success) return accounts;

            accounts.AddRange(result.Data.FirstOrDefault()?.PositionBalances.ToList() ?? new());

            return accounts;
        }
        private async static Task<IEnumerable<CoinbaseAccount>> GetCoinbaseAvailableAccounts(ICoinbaseRestClient client)
        {
            WebCallResult<CoinbaseAccountPage>? result = null;
            List<CoinbaseAccount> accounts = new();
            var cursor = "";
            do
            {
                result = await client.AdvancedTradeApi.Account.GetAccountsAsync(250, cursor);

                if (!result.Success) break;

                accounts.AddRange(result.Data.Accounts.Where(x => x.AvailableBalance.Value > 0).ToList());
                cursor = result.Data.Cursor;
            }
            while (result.Data.HasNextPage);

            return accounts;
        }
        private async static Task<List<Wallet>> GetBitpandaAccounts(HttpClient client)
        {
            var response = await client.GetAsync("https://api.bitpanda.com/v1/asset-wallets");

            if (!response.IsSuccessStatusCode) throw new Exception("no success");

            var json = await response.Content.ReadAsStringAsync();

            var list = JsonSerializer.Deserialize<BitpandaAssetWallet>(json);

            return list?.Data.Attributes.Cryptocoin.Attributes.Wallets.Where(x => Convert.ToDecimal(x.Attributes.Balance) > 0).ToList() ?? new();
        }
    }

    public struct BalanceResult
    {
        public string Symbol { get; set; }
        public decimal Balance { get; set; }
    }
}
