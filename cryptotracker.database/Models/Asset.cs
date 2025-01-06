using System.ComponentModel.DataAnnotations;

namespace cryptotracker.database.Models
{
    public class Asset
    {
        [Key]
        public string Symbol { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
    }
}
