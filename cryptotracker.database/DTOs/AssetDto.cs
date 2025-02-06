using cryptotracker.database.Models;

namespace cryptotracker.database.DTOs
{
    public class AssetDto
    {
        public required string Symbol { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public bool IsHidden { get; set; }

        public static AssetDto FromModel(Asset asset)
        {
            return new AssetDto()
            {
                Symbol = asset.Symbol,
                Name = asset.Name,
                Image = asset.Image,
                IsHidden = asset.IsHidden
            };
        }
    }
}
