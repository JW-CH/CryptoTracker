
using cryptotracker.core.Interfaces;

namespace cryptotracker.core.Models
{
    public class CryptoTrackerConfig : ICryptoTrackerConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int Interval { get; set; } = 60;
        public CryptoTrackerAuth Auth { get; set; } = new();
        public CryptoTrackerOidc Oidc { get; set; } = new();
        public string LogLevel { get; set; } = "Information";
        public StockApi? StockApi { get; set; } = null;
        public int MaxFillDays { get; set; } = 10;
        public string Timezone { get; set; } = "Europe/Zurich";

        private string _baseCurrency = "chf";
        public string BaseCurrency
        {
            get => _baseCurrency;
            set => _baseCurrency = string.IsNullOrWhiteSpace(value) ? "chf" : value.ToLowerInvariant();
        }

        public List<CryptoTrackerIntegration> Integrations { get; set; } = new();
    }
}
