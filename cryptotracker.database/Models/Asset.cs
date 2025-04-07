using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace cryptotracker.database.Models
{
    public class Asset
    {
        [Key]
        public required string Symbol { get; set; }
        public string? ExternalId { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public required AssetType AssetType { get; set; }
        public required bool IsHidden { get; set; }
    }

    public enum AssetType
    {
        [Description("Fiat")]
        Fiat,
        [Description("Crypto")]
        Crypto,
        [Description("Stock")]
        Stock,
        [Description("ETF")]
        ETF,
        [Description("Commodity")]
        Commodity,
        [Description("Real Estate")]
        RealEstate
    }
}
