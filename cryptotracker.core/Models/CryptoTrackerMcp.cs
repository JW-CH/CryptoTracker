namespace cryptotracker.core.Models
{
    public class CryptoTrackerMcp
    {
        public bool Enabled { get; set; }

        /// <summary>Bearer secret for the MCP endpoint. Required when <see cref="Enabled"/> is true. Not a session JWT.</summary>
        public string? Token { get; set; }

        public CryptoTrackerMcp()
        {
            Enabled = false;
        }
    }
}
