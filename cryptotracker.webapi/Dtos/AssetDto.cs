using cryptotracker.database.Models;

namespace cryptotracker.webapi.Dtos
{
    public class AssetDto
    {
        public required string Symbol { get; set; }
        public string? ExternalId { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public required AssetType AssetType { get; set; }
        public bool IsHidden { get; set; }

        public static AssetDto FromModel(Asset asset)
        {
            return new AssetDto()
            {
                Symbol = asset.Symbol,
                ExternalId = asset.ExternalId,
                Name = asset.Name,
                Image = asset.Image,
                AssetType = asset.AssetType,
                IsHidden = asset.IsHidden
            };
        }
    }
}
