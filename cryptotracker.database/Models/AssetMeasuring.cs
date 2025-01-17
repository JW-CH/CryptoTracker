
using System.ComponentModel.DataAnnotations.Schema;

namespace cryptotracker.database.Models
{
    public class AssetMeasuring
    {
        public Guid Id { get; set; }
        [ForeignKey(nameof(Asset))]
        public required string Symbol { get; set; }
        public Asset? Asset { get; set; }
        public required Guid IntegrationId { get; set; }
        public ExchangeIntegration? Integration { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Amount { get; set; }
    }
}
