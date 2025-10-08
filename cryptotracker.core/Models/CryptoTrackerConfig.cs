
using System.Text.Json;
using cryptotracker.core.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace cryptotracker.core.Models
{
    public class CryptoTrackerConfig : ICryptoTrackerConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int Interval { get; set; } = 60;
        public CryptoTrackerAuth Auth { get; set; } = new();
        public CryptoTrackerOidc Oidc { get; set; } = new();
        public string LogLevel { get; set; } = "Information";
        public string? StockApi { get; set; } = "";
        public List<CryptoTrackerIntegration> Integrations { get; set; } = new();

        public static CryptoTrackerConfig LoadFromJson(string input)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<CryptoTrackerConfig>(input, options)
                   ?? throw new InvalidOperationException("Unable to deserialize CryptoTracker config from JSON.");
        }

        public static CryptoTrackerConfig LoadFromYml(string input)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(LowerCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<CryptoTrackerConfig>(input)
                   ?? throw new InvalidOperationException("Unable to deserialize CryptoTracker config from YAML.");
        }
    }
}
