using cryptotracker.core.Models;

namespace cryptotracker.core.Logic
{
    public interface ICryptoTrackerLogic
    {
        Task<IEnumerable<BalanceResult>> GetAvailableIntegrationBalances(CryptoTrackerIntegration integration);
    }
}
