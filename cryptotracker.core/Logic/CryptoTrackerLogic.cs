using Coinbase.Net.Interfaces.Clients;
using Coinbase.Net.Objects.Models;
using CryptoClients.Net.Interfaces;
using CryptoExchange.Net.Objects;
using cryptotracker.core.Models;
using ImmichFrame.Core.Helpers;
using System.Text.Json;

namespace cryptotracker.core.Logic
{
    public static class CryptoTrackerLogic
    {
        public async static Task<IEnumerable<CoinbaseAccount>> GetCoinbaseAvailableAccounts(ICoinbaseRestClient client)
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
        public async static Task<List<Wallet>> GetBitpandaAccounts(HttpClient client)
        {
            var response = await client.GetAsync("https://api.bitpanda.com/v1/asset-wallets");

            if (!response.IsSuccessStatusCode) throw new Exception("no success");

            var json = await response.Content.ReadAsStringAsync();

            var list = JsonSerializer.Deserialize<BitpandaAssetWallet>(json);

            return list?.Data.Attributes.Cryptocoin.Attributes.Wallets.Where(x => Convert.ToDecimal(x.Attributes.Balance) > 0).ToList() ?? new ();
        }
    }
}
