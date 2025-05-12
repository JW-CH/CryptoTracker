
using cryptotracker.core.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace cryptotracker.core.Models
{
    public class CryptotrackerConfig : ICryptotrackerConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int Interval { get; set; } = 60;
        public string LogLevel { get; set; } = "Information";
        public string? StockApi { get; set; } = "";
        public List<CryptotrackerIntegration> Integrations { get; set; } = new();

        public static CryptotrackerConfig LoadFromJson(string input)
        {
            throw new NotImplementedException();
        }

        public static CryptotrackerConfig LoadFromYml(string input)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(LowerCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<CryptotrackerConfig>(input);
        }
    }
}
