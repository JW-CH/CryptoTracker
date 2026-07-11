using cryptotracker.database.Models;

namespace cryptotracker.webapi.Dtos
{
    public class DailyHoldingDto
    {
        public required Guid IntegrationId { get; set; }
        public required string Symbol { get; set; }
        public DateOnly Date { get; set; }
        public decimal Amount { get; set; }
        public HoldingSource Source { get; set; }

        public static DailyHoldingDto FromModel(DailyHolding holding)
        {
            return new DailyHoldingDto()
            {
                IntegrationId = holding.IntegrationId,
                Symbol = holding.Symbol,
                Date = holding.Date,
                Amount = holding.Amount,
                Source = holding.Source
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

        public static AssetHoldingDto SumFromModels(Asset asset, List<DailyHolding> holdings, decimal price)
        {
            var integrationValues = holdings
                .GroupBy(x => x.Integration)
                .Select(x => new IntegrationAmount
                {
                    Integration = IntegrationDto.FromModel(x.Key),
                    Amount = x.Sum(y => y.Amount)
                }).ToList();

            var amt = holdings.Sum(x => x.Amount);

            return new AssetHoldingDto()
            {
                Asset = AssetDto.FromModel(asset),
                IntegrationValues = integrationValues,
                TotalAmount = amt,
                Price = price,
                TotalValue = amt * price,
            };
        }
    }

    public struct IntegrationAmount()
    {
        public required IntegrationDto Integration { get; set; }
        public required decimal Amount { get; set; }
    }
}
