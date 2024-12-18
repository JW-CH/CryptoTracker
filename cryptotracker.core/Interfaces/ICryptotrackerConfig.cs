using cryptotracker.core.Models;

namespace cryptotracker.core.Interfaces
{
    public interface ICryptotrackerConfig
    {
        public List<CryptotrackerIntegration> Integrations { get; set; }
    }
}
