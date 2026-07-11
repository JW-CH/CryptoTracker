
using System.ComponentModel.DataAnnotations.Schema;

namespace cryptotracker.database.Models
{
    public enum HoldingSource
    {
        Sync,
        Manual
    }

    /// <summary>
    /// One snapshot per (integration, asset, portfolio day).
    /// PK is the natural key (IntegrationId, Symbol, Date) — imports upsert into it.
    /// </summary>
    public class DailyHolding
    {
        public required Guid IntegrationId { get; set; }
        public ExchangeIntegration Integration { get; set; } = null!;
        [ForeignKey(nameof(Asset))]
        public required string Symbol { get; set; }
        public Asset? Asset { get; set; }
        public DateOnly Date { get; set; }
        public decimal Amount { get; set; }
        public HoldingSource Source { get; set; }
        // when the snapshot was written (audit only, not part of any day logic)
        public DateTime RecordedAtUtc { get; set; }
    }
}
