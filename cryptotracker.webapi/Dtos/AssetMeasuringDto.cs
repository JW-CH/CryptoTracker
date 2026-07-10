using cryptotracker.database.Models;

namespace cryptotracker.webapi.Dtos
{
    public class AssetMeasuringDto
    {
        public Guid Id { get; set; }
        public required string Symbol { get; set; }
        public required Guid IntegrationId { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Amount { get; set; }

        public static AssetMeasuringDto FromModel(AssetMeasuring measuring)
        {
            return new AssetMeasuringDto()
            {
                Id = measuring.Id,
                Symbol = measuring.Symbol,
                IntegrationId = measuring.IntegrationId,
                Timestamp = measuring.Timestamp,
                Amount = measuring.Amount
            };
        }
    }

    public class AssetHoldingDto
    {
        public required AssetDto Asset { get; set; }
        public required decimal Price { get; set; }
        public required decimal TotalAmount { get; set; }
        public required decimal TotalValue { get; set; }
        public required List<IntegrationAmount> IntegrationValues { get; set; }

        public static AssetHoldingDto SumFromModels(Asset asset, List<AssetMeasuring> measurings, decimal price)
        {
            var groupedMeasurings = measurings.GroupBy(x => x.Integration);
            var integrationValues = groupedMeasurings.Select(x => new IntegrationAmount
            {
                Integration = IntegrationDto.FromModel(x.Key),
                Amount = x.Sum(y => y.Amount)
            }).ToList();

            var amt = measurings.Sum(x => x.Amount);

            return new AssetHoldingDto()
            {
                Asset = AssetDto.FromModel(asset),
                IntegrationValues = integrationValues,
                TotalAmount = amt,
                Price = price,
                TotalValue = amt * price,
            };
        }
        public static AssetHoldingDto FromModel(AssetMeasuring measuring, decimal price)
        {
            if (measuring.Asset == null) throw new Exception("Asset is null");

            List<IntegrationAmount> integrationValues = new()
            {
                new IntegrationAmount()
                {
                    Integration = IntegrationDto.FromModel(measuring.Integration),
                    Amount = measuring.Amount
                }
            };

            return new AssetHoldingDto()
            {
                Asset = AssetDto.FromModel(measuring.Asset),
                IntegrationValues = integrationValues,
                TotalAmount = measuring.Amount,
                Price = price,
                TotalValue = measuring.Amount * price,
            };
        }
    }

    public struct IntegrationAmount()
    {
        public required IntegrationDto Integration { get; set; }
        public required decimal Amount { get; set; }
    }
}
