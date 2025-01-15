using cryptotracker.database.Models;

namespace cryptotracker.database.DTOs
{
    public class AssetDto
    {
        public required string Id { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public bool IsHidden { get; set; }

        public static AssetDto FromModel(Asset asset)
        {
            return new AssetDto()
            {
                Id = asset.Symbol,
                Name = asset.Name,
                Image = asset.Image,
                IsHidden = asset.IsHidden
            };
        }
    }

    public class AssetMeasuringDto
    {
        public required AssetDto Asset { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalValue { get; set; }

        public static AssetMeasuringDto SumFromModels(Asset asset, List<AssetMeasuring> measurings, decimal price)
        {
            var amt = measurings.Sum(x => x.StandingValue);

            return new AssetMeasuringDto()
            {
                Asset = AssetDto.FromModel(asset),
                Amount = amt,
                Price = price,
                TotalValue = amt * price,
            };
        }
        public static AssetMeasuringDto FromModel(AssetMeasuring measuring, decimal price)
        {
            if (measuring.Asset == null) throw new Exception("Asset is null");

            return new AssetMeasuringDto()
            {
                Asset = AssetDto.FromModel(measuring.Asset),
                Amount = measuring.StandingValue,
                Price = price,
                TotalValue = measuring.StandingValue * price,
            };
        }
    }
}
