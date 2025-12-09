using cryptotracker.core.Models;

namespace cryptotracker.core.Logic
{
    public interface ICryptoTrackerLogic
    {
        Task<IEnumerable<BalanceResult>> GetAvailableIntegrationBalances(CryptoTrackerIntegration integration);
        Task<List<AssetMetadata>> GetCoinData(string currency, List<string> coinIds);
        Task<List<Coin>> GetCoinList();
    }
}
