using Coinbase.Net.Clients;
using Coinbase.Net.Interfaces.Clients;
using Coinbase.Net.Objects.Models;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;

namespace cryptotracker.core.Logic.Integrations;

public class CoinbaseIntegrationProvider : IIntegrationProvider
{
    public CryptoTrackerIntegrationType Type => CryptoTrackerIntegrationType.Coinbase;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegration integration)
    {
        using var client = new CoinbaseRestClient(xy =>
        {
            xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
        });

        var accounts = await GetCoinbaseAvailableAccounts(client);

        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.AvailableBalance.Value + account.HoldBalance.Value }).ToList();
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
}
