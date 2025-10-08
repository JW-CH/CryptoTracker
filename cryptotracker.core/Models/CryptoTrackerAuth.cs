namespace cryptotracker.core.Models
{
    public class CryptoTrackerAuth
    {
        public string? Secret { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int ExpiryMinutes { get; set; }

        public CryptoTrackerAuth()
        {
            ExpiryMinutes = 60;
        }
    }
}
