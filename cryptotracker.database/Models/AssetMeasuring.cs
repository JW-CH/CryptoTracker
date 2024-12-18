
namespace cryptotracker.database.Models
{
    public class AssetMeasuring
    {
        public Guid Id { get; set; }
        public Asset Asset { get; set; }
        public ExchangeIntegration Integration { get; set; }
        public DateTime StandingDate { get; set; }
        public decimal StandingValue { get; set; }
    }
}
