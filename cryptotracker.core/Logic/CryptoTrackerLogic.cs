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
using ImmichFrame.Core.Helpers;
using Kucoin.Net;
using Kucoin.Net.Clients;
using Kucoin.Net.Interfaces.Clients;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Spot;
using Microsoft.Extensions.Logging;
using NBitcoin;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace cryptotracker.core.Logic
{
    public class CryptoTrackerLogic
    {
        private ILogger _logger;
        public CryptoTrackerLogic(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<BalanceResult>> GetAvailableIntegrationBalances(CryptotrackerIntegration integration)
        {
            _logger.LogTrace($"Fetching balances for integration {integration.Name}");

            switch (integration.Type.ToLower())
            {
                case "bitpanda":
                    using (var bitpandaClient = new HttpClient())
                    {
                        var result = new List<BalanceResult>();

                        bitpandaClient.UseApiKey(integration.Secret);

                        //var full = await GetBitpandaPortfolio(bitpandaClient);
                        // result.AddRange(full.Select(x => new BalanceResult { Symbol = x.Attributes.AssetSymbol, Balance = Convert.ToDecimal(x.Attributes.AssetBalance) }).ToList());

                        var accounts = await GetBitpandaAccounts(bitpandaClient);
                        var fiat = await GetBitpandaFiatAccounts(bitpandaClient);

                        result.AddRange(accounts.Select(account => new BalanceResult { Symbol = account.Attributes.CryptocoinSymbol, Balance = Convert.ToDecimal(account.Attributes.Balance) }).ToList());
                        result.AddRange(fiat.Select(account => new BalanceResult { Symbol = account.Attributes.FiatSymbol, Balance = Convert.ToDecimal(account.Attributes.Balance) }).ToList());

                        return result;
                    }
                case "cryptocom":
                    using (var cryptocomClient = new CryptoComRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                    }))
                    {
                        var accounts = await GetCryptoComAvailableAccounts(cryptocomClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Quantity }).ToList();
                    }
                case "kucoin":
                    using (var kucoinClient = new KucoinRestClient(xy =>
                    {
                        xy.ApiCredentials = new KucoinApiCredentials(integration.Key, integration.Secret, integration.Passphrase);
                    }))
                    {
                        var accounts = await GetKucoinAvailableAccounts(kucoinClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Total }).ToList();
                    }
                case "coinbase":
                    using (var coinbaseClient = new CoinbaseRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                    }))
                    {
                        var accounts = await GetCoinbaseAvailableAccounts(coinbaseClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.AvailableBalance.Value + account.HoldBalance.Value }).ToList();
                    }
                case "binance":
                    using (var binanceClient = new BinanceRestClient(xy =>
                    {
                        xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                    }))
                    {
                        var accounts = await GetBinanceAvailableAccounts(binanceClient);

                        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Total }).ToList();
                    }
                case "bitcoin":
                case "btc":
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "BTC",
                            Balance = await GetBitcoinAvailableBalances(client, integration.Key)
                        }};
                    }
                case "ethereum":
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "ETH",
                            Balance = await GetEthereumAvailableBalances(client, integration.Key)
                        }};
                    }
                case "ripple":
                case "xrp":
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "XRP",
                            Balance = await GetRippleAvailableBalances(client, integration.Key)
                        }};
                    }
                case "cardano":
                case "ada":
                    using (HttpClient client = new HttpClient())
                    {
                        return new List<BalanceResult>() { new BalanceResult(){
                            Symbol = "ADA",
                            Balance = await GetCardanoAvailableBalances(client, integration.Key)
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
                _logger.LogError($"Failed to fetch balance for address {address}: {response.StatusCode}");
                return 0;
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
                _logger.LogError($"Failed to fetch balance for address {address}: {response.StatusCode}");
                return 0;
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
                    _logger.LogError($"Failed to fetch balance for address {address}: {response.StatusCode}");
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
        private async Task<List<KucoinAccount>> GetKucoinAvailableAccounts(IKucoinRestClient client)
        {
            WebCallResult<IEnumerable<KucoinAccount>>? result = null;
            List<KucoinAccount> accounts = new();

            result = await client.SpotApi.Account.GetAccountsAsync();

            if (!result.Success)
            {
                _logger.LogError($"Could not get balances for Kucoin integration");
                return accounts;
            }

            accounts.AddRange(result.Data.ToList() ?? new());

            return accounts;
        }
        private async Task<IEnumerable<CryptoComBalance>> GetCryptoComAvailableAccounts(ICryptoComRestClient client)
        {
            WebCallResult<IEnumerable<CryptoComBalances>>? result = null;
            List<CryptoComBalance> accounts = new();

            result = await client.ExchangeApi.Account.GetBalancesAsync();

            if (!result.Success)
            {
                _logger.LogError($"Could not get balances for CryptoCom integration");
                return accounts;
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
                    _logger.LogError($"Could not get balances for Coinbase integration");
                    return new List<CoinbaseAccount>();
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
                _logger.LogError($"Could not get balances for Binance integration");
                return accounts;
            }

            accounts.AddRange(result.Data.Balances.Where(x => x.Total > 0).ToList() ?? new());

            return accounts;
        }
        private async Task<List<BitpandaFiatWallet>> GetBitpandaFiatAccounts(HttpClient client)
        {
            var response = await client.GetAsync("https://api.bitpanda.com/v1/fiatwallets");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch fiat balances for Bitpanda: {response.StatusCode}");
                _logger.LogError(await response.Content.ReadAsStringAsync());
                return new();
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
                _logger.LogError($"Failed to fetch balances for Bitpanda: {response.StatusCode}");
                _logger.LogError(await response.Content.ReadAsStringAsync());
                return new();
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
                _logger.LogError($"Failed to fetch account balances for Bitpanda: {response.StatusCode}");
                _logger.LogError(await response.Content.ReadAsStringAsync());
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<BitpandaAssetWallet>(json);
            return list?.Data.Attributes.Cryptocoin.Attributes.Wallets.Where(x => Convert.ToDecimal(x.Attributes.Balance) > 0).ToList() ?? new();
        }
        public async Task<List<AssetMetadata>> GetFiatData(string currency, List<string> fiatIds)
        {
            fiatIds = fiatIds.Distinct().Select(x => x.ToLower()).ToList();

            var result = new List<AssetMetadata>();

            if (fiatIds.Count == 0) return result;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "cryptotracker");
            string apiUrl = $"https://api.frankfurter.app/latest?base={currency}&symbols={string.Join(",", fiatIds)}";
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

            var fiatList = await GetFiatList();

            foreach (var item in rates)
            {
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
                    Currency = currency,
                    Name = name,
                    Price = price
                });
            }

            if (fiatIds.Contains(currency.ToLower()))
            {
                result.Add(new AssetMetadata()
                {
                    AssetId = currency,
                    Symbol = currency,
                    Image = "",
                    Currency = currency,
                    Name = "",
                    Price = 1
                });
            }

            return result;
        }
        public async Task<List<AssetMetadata>> GetCoinData(string currency, List<string> coinIds)
        {
            var result = new List<AssetMetadata>();

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "cryptotracker");
            string apiUrl = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency={currency}&ids={string.Join(",", coinIds)}";

            var response = await client.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch Coin balances: {response.StatusCode}");
                _logger.LogError(await response.Content.ReadAsStringAsync());
                return result;
            }

            var data = JsonSerializer.Deserialize<List<JsonElement>>(await response.Content.ReadAsStringAsync());

            if (data == null)
            {
                _logger.LogError($"Failed to fetch Coin balances: No balances were returned");
                return result;
            }

            foreach (var item in data)
            {
                var id = item.GetProperty("id").GetString() ?? "";
                var name = item.GetProperty("name").GetString() ?? "";
                var image = item.GetProperty("image").GetString() ?? "";
                var symbol = item.GetProperty("symbol").GetString() ?? "";
                var price = item.GetProperty("current_price").GetDecimal();

                result.Add(new AssetMetadata()
                {
                    AssetId = id,
                    Symbol = symbol,
                    Image = image,
                    Currency = currency,
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
        private List<Coin>? _coinList;
        public async Task<List<Coin>> GetCoinList()
        {
            if (_coinList != null) return _coinList;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "cryptotracker");
            var url = "https://api.coingecko.com/api/v3/coins/list";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch Coin list: {response.StatusCode}");
                _logger.LogError(await response.Content.ReadAsStringAsync());
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<List<Coin>>(json);

            if (data == null)
            {
                _logger.LogError($"Failed to fetch Coin list");
                return new();
            }

            _coinList = data;

            return _coinList;
        }
    }

    public struct Fiat
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
    }
    public struct Coin
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
    public struct AssetMetadata
    {
        public string AssetId { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Image { get; set; }
        public string Currency { get; set; }
        public decimal Price { get; set; }
    }
    public struct BalanceResult
    {
        public string Symbol { get; set; }
        public decimal Balance { get; set; }
    }
}
