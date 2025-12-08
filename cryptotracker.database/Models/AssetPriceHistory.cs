using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.database.Models
{
    [PrimaryKey(nameof(Symbol), nameof(Date), nameof(Currency))]
    public class AssetPriceHistory
    {
        [ForeignKey(nameof(Asset))]
        public required string Symbol { get; set; }
        public Asset? Asset { get; set; }
        public required DateOnly Date { get; set; }
        public required string Currency { get; set; }
        public required decimal Price { get; set; }
    }
}
