using cryptotracker.core.Models;

namespace cryptotracker.core.Interfaces
{
    public interface ICryptotrackerConfig
    {
        public List<CryptotrackerIntegration> Integrations { get; set; }
        public CryptoTrackerOidc Oidc { get; set; }
        public string ConnectionString { get; set; }
        public string LogLevel { get; set; }
        public int Interval { get; set; }
        public string? StockApi { get; set; }
    }
}
