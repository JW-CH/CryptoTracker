using cryptotracker.core.Interfaces;

namespace cryptotracker.webapi.Services
{
    /// <summary>
    /// Single source of truth for "which portfolio day is it". Storage stays UTC;
    /// the day boundary is derived from the configured timezone at exactly this
    /// one place instead of ad-hoc DateTime.Now/UtcNow calls.
    /// </summary>
    public class PortfolioClock
    {
        private readonly TimeProvider _timeProvider;
        private readonly TimeZoneInfo _timeZone;

        public PortfolioClock(TimeProvider timeProvider, ICryptoTrackerConfig config)
        {
            _timeProvider = timeProvider;
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.Timezone);
        }

        public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public DateOnly Today => ToPortfolioDay(UtcNow);

        public DateOnly ToPortfolioDay(DateTime timestamp)
        {
            var utc = NormalizeUtc(timestamp);
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone));
        }

        /// <summary>UTC instant at which the given portfolio day begins.</summary>
        public DateTime StartOfDayUtc(DateOnly day)
        {
            var localMidnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localMidnight, _timeZone);
        }

        /// <summary>
        /// Client-supplied DateTimes arrive with any Kind; Unspecified is treated as
        /// UTC because Npgsql rejects non-UTC values on timestamptz columns.
        /// </summary>
        public static DateTime NormalizeUtc(DateTime timestamp) => timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
        };
    }
}
