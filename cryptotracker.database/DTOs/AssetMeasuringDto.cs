using cryptotracker.database.Models;

namespace cryptotracker.database.DTOs
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

    public class MessungDto
    {
        public required AssetDto Asset { get; set; }
        public required decimal Price { get; set; }
        public required decimal TotalAmount { get; set; }
        public required decimal TotalValue { get; set; }
        public required List<IntegrationShit> IntegrationValues { get; set; }

        public static MessungDto SumFromModels(Asset asset, List<AssetMeasuring> measurings, decimal price)
        {
            var groupedMeasurings = measurings.GroupBy(x => x.Integration);
            var integrationValues = groupedMeasurings.Select(x => new IntegrationShit
            {
                Integration = IntegrationDto.FromModel(x.Key),
                Amount = x.Sum(y => y.Amount)
            }).ToList();

            var amt = measurings.Sum(x => x.Amount);

            return new MessungDto()
            {
                Asset = AssetDto.FromModel(asset),
                IntegrationValues = integrationValues,
                TotalAmount = amt,
                Price = price,
                TotalValue = amt * price,
            };
        }
        public static MessungDto FromModel(AssetMeasuring measuring, decimal price)
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

            return new MessungDto()
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
