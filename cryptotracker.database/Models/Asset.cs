using System.ComponentModel.DataAnnotations;

namespace cryptotracker.database.Models
{
    public class Asset
    {
        [Key]
        public required string Symbol { get; set; }
        public required string ExternalId { get; set; }
        public required string Name { get; set; }
        public required string Image { get; set; }
        public required bool IsFiat { get; set; }
        public required bool IsHidden { get; set; }
    }
}
