using System.Text.Json.Serialization;
using cryptotracker.core.Models;

namespace cryptotracker.core.Interfaces
{
    public interface ICryptoTrackerConfig
    {
        public List<CryptoTrackerIntegration> Integrations { get; set; }
        public CryptoTrackerAuth Auth { get; set; }
        public CryptoTrackerOidc Oidc { get; set; }
        public string ConnectionString { get; set; }
        public string LogLevel { get; set; }
        public int Interval { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StockApi? StockApi { get; set; }
        public int MaxFillDays { get; set; }
        public string BaseCurrency { get; set; }
        public string Timezone { get; set; }
    }

    public enum StockApi
    {
        YahooFinance
    }
}
