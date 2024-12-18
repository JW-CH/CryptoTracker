
using cryptotracker.core.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace cryptotracker.core.Models
{
    public class CryptotrackerConfig : ICryptotrackerConfig
    {
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
