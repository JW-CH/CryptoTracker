using cryptotracker.database.Models;

namespace cryptotracker.database.DTOs
{
    public class AssetMeasuringDto
    {
        public required AssetDto Asset { get; set; }
        public required decimal Price { get; set; }
        public required decimal TotalAmount { get; set; }
        public required decimal TotalValue { get; set; }
        public required List<IntegrationShit> IntegrationValues { get; set; }

        public static AssetMeasuringDto SumFromModels(Asset asset, List<AssetMeasuring> measurings, decimal price)
        {
            var groupedMeasurings = measurings.GroupBy(x => x.Integration);
            var integrationValues = groupedMeasurings.Select(x => new IntegrationShit
            {
                Integration = IntegrationDto.FromModel(x.Key),
                Amount = x.Sum(y => y.Amount)
            }).ToList();

            var amt = measurings.Sum(x => x.Amount);

            return new AssetMeasuringDto()
            {
                Asset = AssetDto.FromModel(asset),
                IntegrationValues = integrationValues,
                TotalAmount = amt,
                Price = price,
                TotalValue = amt * price,
            };
        }
        public static AssetMeasuringDto FromModel(AssetMeasuring measuring, decimal price)
        {
            if (measuring.Asset == null) throw new Exception("Asset is null");

            List<IntegrationShit> integrationValues = new()
            {
                new IntegrationShit()
                {
                    Integration = IntegrationDto.FromModel(measuring.Integration),
                    Amount = measuring.Amount
                }
            };

            return new AssetMeasuringDto()
            {
                Asset = AssetDto.FromModel(measuring.Asset),
                IntegrationValues = integrationValues,
                TotalAmount = measuring.Amount,
                Price = price,
                TotalValue = measuring.Amount * price,
            };
        }
    }

    public struct IntegrationShit()
    {
        public required IntegrationDto Integration { get; set; }
        public required decimal Amount { get; set; }
    }
}
