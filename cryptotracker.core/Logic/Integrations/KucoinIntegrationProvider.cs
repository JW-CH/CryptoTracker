using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using Kucoin.Net.Clients;
using Kucoin.Net.Interfaces.Clients;
using Kucoin.Net.Objects.Models.Spot;

namespace cryptotracker.core.Logic.Integrations;

public class KucoinIntegrationProvider : IIntegrationProvider
{
    public CryptoTrackerIntegrationType Type => CryptoTrackerIntegrationType.Kucoin;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegrationSource source)
    {
        using var client = new KucoinRestClient(xy =>
        {
            xy.ApiCredentials = new ApiCredentials(source.Key, source.Secret, source.Passphrase);
        });

        var accounts = await GetKucoinAvailableAccounts(client);

        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Total }).ToList();
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
}
