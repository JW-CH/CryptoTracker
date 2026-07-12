using System.Text.Json.Serialization;

namespace cryptotracker.core.Models
{
    public enum CryptoTrackerIntegrationType
    {
        Unknown,
        Bitpanda,
        Cryptocom,
        Kucoin,
        Coinbase,
        Binance,
        Bitcoin,
        Ethereum,
        Ripple,
        Cardano,
    }

    public class CryptoTrackerIntegration
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<CryptoTrackerIntegrationSource> Sources { get; set; } = new();
    }

    public class CryptoTrackerIntegrationSource
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CryptoTrackerIntegrationType Type { get; set; } = CryptoTrackerIntegrationType.Unknown;
        public string Key { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string Passphrase { get; set; } = string.Empty;
    }
}
