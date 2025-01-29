
using cryptotracker.database.Models;

namespace cryptotracker.database.DTOs
{
    public class IntegrationDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public bool IsHidden { get; set; }
        public bool IsManual { get; set; }

        public static IntegrationDto FromModel(ExchangeIntegration integration)
        {
            return new IntegrationDto()
            {
                Id = integration.Id,
                Name = integration.Name,
                Description = integration.Description,
                IsHidden = integration.IsHidden,
                IsManual = integration.IsManual
            };

        }
    }
}