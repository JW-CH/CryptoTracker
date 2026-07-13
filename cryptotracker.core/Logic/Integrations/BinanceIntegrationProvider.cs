using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;

namespace cryptotracker.core.Logic.Integrations;

public class BinanceIntegrationProvider : IIntegrationProvider
{
    public CryptoTrackerIntegrationType Type => CryptoTrackerIntegrationType.Binance;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegrationSource source)
    {
        using var client = new BinanceRestClient(xy =>
        {
            xy.ApiCredentials = new BinanceCredentials(source.Key, source.Secret);
        });

        var accounts = await GetBinanceAvailableAccounts(client);

        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Total }).ToList();
    }

    private async Task<IEnumerable<BinanceBalance>> GetBinanceAvailableAccounts(BinanceRestClient client)
    {
        HttpResult<BinanceAccountInfo>? result = null;
        List<BinanceBalance> accounts = new();

        result = await client.SpotApi.Account.GetAccountInfoAsync();

        if (!result.Success)
        {
            throw new InvalidOperationException($"Could not get balances for Binance integration: {result.Error?.Message}");
        }

        accounts.AddRange(result.Data.Balances.Where(x => x.Total > 0).ToList() ?? new());

        return accounts;
    }
}
