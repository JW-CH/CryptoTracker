using Coinbase.Net.Clients;
using Coinbase.Net.Interfaces.Clients;
using Coinbase.Net.Objects.Models;
using CryptoCom.Net.Clients;
using CryptoCom.Net.Interfaces.Clients;
using CryptoCom.Net.Objects.Models;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using cryptotracker.core.Models;
using ImmichFrame.Core.Helpers;
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
                default:
                    throw new NotImplementedException($"Integration {integration.Type} was not implemented!");
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

    public enum IntegrationEnum
    {
        cryptocom,
        coinbase,
        binance,
        bitpanda
    }
}
