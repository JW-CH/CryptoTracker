using cryptotracker.core.Models;
using cryptotracker.database.Models;

namespace cryptotracker.core.Interfaces;

public interface IIntegrationProvider
{
    CryptoTrackerIntegrationType Type { get; }
    Task<IEnumerable<BalanceResult>> GetBalancesAsync(CryptoTrackerIntegrationSource source);
}

public struct BalanceResult
{
    public string Symbol { get; set; }
    public decimal Balance { get; set; }
    public AssetType? AssetType { get; set; }
}