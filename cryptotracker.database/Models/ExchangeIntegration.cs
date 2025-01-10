namespace cryptotracker.database.Models
{
    public class ExchangeIntegration
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public bool IsManual { get; set; }
    }
}
