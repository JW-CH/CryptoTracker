namespace cryptotracker.core.Models
{
    public class CryptoTrackerOidc
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Authority { get; set; }

        public bool IsEnabled => !string.IsNullOrWhiteSpace(Authority) && !string.IsNullOrWhiteSpace(ClientId);

        public CryptoTrackerOidc()
        {
            ClientId = string.Empty;
            ClientSecret = string.Empty;
            Authority = string.Empty;
        }
    }
}
