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
        public string Name { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CryptoTrackerIntegrationType Type { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
        public string Passphrase { get; set; }
        public string Description { get; set; }

        public CryptoTrackerIntegration()
        {
            Name = string.Empty;
            Type = CryptoTrackerIntegrationType.Unknown;
            Key = string.Empty;
            Secret = string.Empty;
            Passphrase = string.Empty;
            Description = string.Empty;
        }
    }
}
