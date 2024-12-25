using cryptotracker.database.Models;

namespace cryptotracker.database.DTOs
{
    public class AssetMeasuringDto
    {
        public string AssetId { get; set; }
        public string AssetName { get; set; }
        public decimal StandingValue { get; set; }

        public static AssetMeasuringDto FromModel(AssetMeasuring model)
        {
            return new AssetMeasuringDto
            {
                AssetId = model.AssetId,
                AssetName = model.Asset.Name,
                StandingValue = model.StandingValue
            };
        }
    }
}
