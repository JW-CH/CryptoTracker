namespace cryptotracker.core.Models
{
    public class CryptoTrackerIntegration
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
        public string Passphrase { get; set; }
        public string Description { get; set; }

        public CryptoTrackerIntegration()
        {
            Name = string.Empty;
            Type = string.Empty;
            Key = string.Empty;
            Secret = string.Empty;
            Passphrase = string.Empty;
            Description = string.Empty;
        }

        public CryptoTrackerIntegration(string name, string type, string key, string secret, string passphrase, string description)
        {
            Name = name;
            Type = type;
            Key = key;
            Secret = secret;
            Passphrase = passphrase;
            Description = description;
        }
    }
}
