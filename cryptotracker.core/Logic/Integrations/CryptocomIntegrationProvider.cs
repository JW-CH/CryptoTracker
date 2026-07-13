using CryptoCom.Net;
using CryptoCom.Net.Clients;
using CryptoCom.Net.Interfaces.Clients;
using CryptoCom.Net.Objects.Models;
using CryptoExchange.Net.Objects;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;

namespace cryptotracker.core.Logic.Integrations;

public class CryptocomIntegrationProvider : IIntegrationProvider
{
    public CryptoTrackerIntegrationType Type => CryptoTrackerIntegrationType.Cryptocom;

    public async Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegrationSource source)
    {
        using var client = new CryptoComRestClient(xy =>
        {
            xy.ApiCredentials = new CryptoComCredentials(source.Key, source.Secret);
        });

        var accounts = await GetCryptoComAvailableAccounts(client);

        return accounts.Select(account => new BalanceResult { Symbol = account.Asset, Balance = account.Quantity }).ToList();
    }

    private async Task<IEnumerable<CryptoComBalance>> GetCryptoComAvailableAccounts(ICryptoComRestClient client)
    {
        HttpResult<CryptoComBalances[]>? result = null;
        List<CryptoComBalance> accounts = new();

        result = await client.ExchangeApi.Account.GetBalancesAsync();

        if (!result.Success)
        {
            throw new InvalidOperationException($"Could not get balances for CryptoCom integration: {result.Error?.Message}");
        }

        accounts.AddRange(result.Data.FirstOrDefault()?.PositionBalances.ToList() ?? new());

        return accounts;
    }
}
