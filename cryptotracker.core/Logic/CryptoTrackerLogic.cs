using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Keys;
using CardanoSharp.Wallet.Utilities;
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
using cryptotracker.database.Models;
using ImmichFrame.Core.Helpers;
using Kucoin.Net.Clients;
using Kucoin.Net.Interfaces.Clients;
using Kucoin.Net.Objects.Models.Spot;
using Microsoft.Extensions.Logging;
using NBitcoin;
using System.Text.Json;

namespace cryptotracker.core.Logic
{
    public class CryptoTrackerLogic : ICryptoTrackerLogic
    {
        private ILogger _logger;
        public CryptoTrackerLogic(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<BalanceResult>> GetAvailableIntegrationBalances(CryptoTrackerIntegration integration)
        {
            _logger.LogTrace($"Fetching balances for integration {integration.Name}");

            switch (integration.Type)
            {
                case CryptoTrackerIntegrationType.Bitpanda:
                    using (var bitpandaClient = new HttpClient())
                    {
                        var result = new List<BalanceResult>();

                        bitpandaClient.UseApiKey(integration.Secret);

                        //var full = await GetBitpandaPortfolio(bitpandaClient);
                        // result.AddRange(full.Select(x => new BalanceResult { Symbol = x.Attributes.AssetSymbol, Balance = Convert.ToDecimal(x.Attributes.AssetBalance) }).ToList());

                        var accounts = await GetBitpandaAccounts(bitpandaClient);
                        var fiat = await GetBitpandaFiatAccounts(bitpandaClient);

                        result.AddRange(accounts.Select(account => new BalanceResult { Symbol = account.Attributes.CryptocoinSymbol, Balance = Convert.ToDecimal(account.Attributes.Balance), AssetType = AssetType.Crypto }).ToList());
                        result.AddRange(fiat.Select(account => new BalanceResult { Symbol = account.Attributes.FiatSymbol, Balance = Convert.ToDecimal(account.Attributes.Balance), AssetType = AssetType.Fiat }).ToList());

                        return result;
                    }
                case CryptoTrackerIntegrationType.Cryptocom:
                    using (var cryptocomClient = new CryptoComRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                    }))
                    {
                        var accounts = await GetCryptoComAvailableAccounts(cryptocomClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Quantity }).ToList();
                    }
                case CryptoTrackerIntegrationType.Kucoin:
                    using (var kucoinClient = new KucoinRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret, integration.Passphrase);
                    }))
                    {
                        var accounts = await GetKucoinAvailableAccounts(kucoinClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Total }).ToList();
                    }
                case CryptoTrackerIntegrationType.Coinbase:
                    using (var coinbaseClient = new CoinbaseRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                    }))
                    {
                        var accounts = await GetCoinbaseAvailableAccounts(coinbaseClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.AvailableBalance.Value + account.HoldBalance.Value }).ToList();
                    }
                case CryptoTrackerIntegrationType.Binance:
                    using (var binanceClient = new BinanceRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                    }))
                    {
                        var accounts = await GetBinanceAvailableAccounts(binanceClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Total }).ToList();
                    }
                case CryptoTrackerIntegrationType.Bitcoin:
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "BTC",
                            Balance = await GetBitcoinAvailableBalances(client, integration.Key),
                            AssetType = AssetType.Crypto
                        }};
                    }
                case CryptoTrackerIntegrationType.Ethereum:
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "ETH",
                            Balance = await GetEthereumAvailableBalances(client, integration.Key),
                            AssetType = AssetType.Crypto
                        }};
                    }
                case CryptoTrackerIntegrationType.Ripple:
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "XRP",
                            Balance = await GetRippleAvailableBalances(client, integration.Key),
                            AssetType = AssetType.Crypto
                        }};
                    }
                case CryptoTrackerIntegrationType.Cardano:
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "ADA",
                            Balance = await GetCardanoAvailableBalances(client, integration.Key),
                            AssetType = AssetType.Crypto
                        }};
                    }
                default:
                    throw new NotImplementedException($"Integration {integration.Type} was not implemented!");
            }
        }
        private async Task<decimal> GetCardanoAvailableBalances(HttpClient client, string input)
        {
            async Task<(decimal balance, int transactions)> GetCardanoAmountFromAddress(HttpClient client, string address)
            {
                throw new NotImplementedException();

                var apiUrl = $"https://api.cardanoscan.io/api/v1/address/balance?address={address}";
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
                    _logger.LogError($"Failed to fetch balance for address {address}: {response.StatusCode}");
                    return (0, 0);
                }
            }

            if (!input.StartsWith("addr", StringComparison.OrdinalIgnoreCase))
            {
                string xpub = input;

                var byteStuff = CardanoHelper.GetByteStuff(xpub);

                var extPubKey = new PublicKey(byteStuff.publicKey, byteStuff.chaincode);

                var keyPath = extPubKey.Derive(CardanoSharp.Wallet.Enums.RoleType.InternalChain);

                decimal totalBalance = 0;
                int i = 0;
                int transactions;
                do
                {
                    var pubKey = keyPath.Derive(i);
                    Address enterpriseAddress = AddressUtility.GetEnterpriseAddress(pubKey.PublicKey, NetworkType.Mainnet);

                    var res = await GetCardanoAmountFromAddress(client, enterpriseAddress.ToString());
                    totalBalance += res.balance;
                    transactions = res.transactions;

                    i++;
                }
                while (transactions > 0);

                return totalBalance;
            }
            else
            {
                return (await GetCardanoAmountFromAddress(client, input)).balance;
            }
        }
        private async Task<decimal> GetRippleAvailableBalances(HttpClient client, string address)
        {
            var apiUrl = $"https://api.xrpscan.com/api/v1/account/{address}";

            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();

                var balance = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("xrpBalance").GetString();

                decimal.TryParse(balance, out decimal result);

                return result;
            }
            else
            {
                throw new InvalidOperationException($"Failed to fetch balance for address {address}: {response.StatusCode}");
            }
        }
        private async Task<decimal> GetEthereumAvailableBalances(HttpClient client, string address)
        {
            string apiUrl = $"https://api.ethplorer.io/getAddressInfo/{address}?apiKey=freekey";

            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();

                var property = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("ETH");

                var balance = property.GetProperty("balance").GetDecimal();

                return balance;
            }
            else
            {
                throw new InvalidOperationException($"Failed to fetch balance for address {address}: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Retrieves the available Bitcoin balances for a given input, which can be either an address or an extended public key (xpub).
        /// </summary>
        /// <param name="client">The HttpClient used to make the request.</param>
        /// <param name="input">The Bitcoin address or extended public key (xpub) to retrieve the balance for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the available balance in BTC.</returns>
        private async Task<decimal> GetBitcoinAvailableBalances(HttpClient client, string input)
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
                    throw new InvalidOperationException($"Failed to fetch balance for address {address}: {response.StatusCode}");
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
                int transactions;
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
        private async Task<List<KucoinAccount>> GetKucoinAvailableAccounts(IKucoinRestClient client)
        {
            WebCallResult<KucoinAccount[]>? result;
            List<KucoinAccount> accounts = new();

            result = await client.SpotApi.Account.GetAccountsAsync();

            if (!result.Success)
            {
                throw new InvalidOperationException($"Could not get balances for Kucoin integration: {result.Error?.Message}");
            }

            accounts.AddRange(result.Data.ToList() ?? new());

            return accounts;
        }
        private async Task<IEnumerable<CryptoComBalance>> GetCryptoComAvailableAccounts(ICryptoComRestClient client)
        {
            WebCallResult<CryptoComBalances[]>? result = null;
            List<CryptoComBalance> accounts = new();

            result = await client.ExchangeApi.Account.GetBalancesAsync();

            if (!result.Success)
            {
                throw new InvalidOperationException($"Could not get balances for CryptoCom integration: {result.Error?.Message}");
            }

            accounts.AddRange(result.Data.FirstOrDefault()?.PositionBalances.ToList() ?? new());

            return accounts;
        }
        private async Task<IEnumerable<CoinbaseAccount>> GetCoinbaseAvailableAccounts(ICoinbaseRestClient client)
        {
            WebCallResult<CoinbaseAccountPage>? result = null;
            List<CoinbaseAccount> accounts = new();
            var cursor = "";
            do
            {
                result = await client.AdvancedTradeApi.Account.GetAccountsAsync(250, cursor);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Could not get balances for Coinbase integration: {result.Error?.Message}");
                }

                accounts.AddRange(result.Data.Accounts.Where(x => x.AvailableBalance.Value > 0).ToList());
                cursor = result.Data.Cursor;
            }
            while (result.Data.HasNextPage);

            return accounts;
        }
        private async Task<IEnumerable<BinanceBalance>> GetBinanceAvailableAccounts(BinanceRestClient client)
        {
            WebCallResult<BinanceAccountInfo>? result = null;
            List<BinanceBalance> accounts = new();

            result = await client.SpotApi.Account.GetAccountInfoAsync();

            if (!result.Success)
            {
                throw new InvalidOperationException($"Could not get balances for Binance integration: {result.Error?.Message}");
            }

            accounts.AddRange(result.Data.Balances.Where(x => x.Total > 0).ToList() ?? new());

            return accounts;
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
    }

    public struct BalanceResult
    {
        public string Symbol { get; set; }
        public decimal Balance { get; set; }
        public AssetType? AssetType { get; set; }
    }
}
