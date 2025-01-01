using Microsoft.EntityFrameworkCore;

namespace cryptotracker.database.Models
{
    [PrimaryKey(nameof(Symbol), nameof(Date), nameof(Currency))]
    public class AssetPriceHistory
    {
        public string Symbol { get; set; }
        public Asset Asset { get; set; }
        public DateTime Date { get; set; }
        public string Currency { get; set; }
        public decimal Price { get; set; }
    }
}
