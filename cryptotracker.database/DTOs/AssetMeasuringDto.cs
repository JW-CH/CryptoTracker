using cryptotracker.database.Models;

namespace cryptotracker.database.DTOs
{
    public class AssetMeasuringDto
    {
        public required string AssetId { get; set; }
        public string? AssetName { get; set; }
        public decimal AssetAmount { get; set; }
        public decimal AssetPrice { get; set; }
        public decimal FiatValue { get; set; }

        public static AssetMeasuringDto FromModel(AssetMeasuring model)
        {
            throw new NotImplementedException();
        }
    }
}
