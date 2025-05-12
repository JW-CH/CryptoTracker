using System.Globalization;
using CsvHelper;
using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;

public class StooqStockLogic : IStockLogic
{
    private readonly ILogger _logger;
    private readonly IFiatLogic _fiatLogic;
    private readonly HttpClient _httpClient;

    public StooqStockLogic(ILogger logger, IFiatLogic fiatLogic)
    {
        _logger = logger;
        _fiatLogic = fiatLogic;
        _httpClient = new HttpClient();
    }

    public Task<IEnumerable<Stock>> GetAllStocksAsync()
    {
        throw new NotImplementedException("Stooq does not support listing all stocks.");
    }

    public async Task<AssetMetadata> GetStockByIdAsync(string currency, string id)
    {
        var result = await GetStocksByIdsAsync(currency, new List<string> { id });
        return result.FirstOrDefault();
    }

    public async Task<List<AssetMetadata>> GetStocksByIdsAsync(string currency, List<string> ids)
    {
        var result = new List<AssetMetadata>();
        var fiatPrices = new Dictionary<string, decimal>();

        foreach (var id in ids)
        {
            var url = $"https://stooq.com/q/d/l/?s={id.ToLower()}.us&i=d";
            _logger.LogTrace($"Downloading CSV from {url}");

            try
            {
                var csv = await _httpClient.GetStringAsync(url);

                using var reader = new StringReader(csv);
                using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);

                var records = csvReader.GetRecords<StooqCsvRecord>().ToList();
                var latest = records.LastOrDefault();

                if (latest == null || latest.Close == 0)
                {
                    _logger.LogWarning($"No valid data found for {id}");
                    continue;
                }

                decimal price = latest.Close;
                _logger.LogTrace($"Latest price for {id}: {price}");
                string stooqCurrency = "USD"; // Stooq liefert keine Währung, wir nehmen USD an

                if (!string.Equals(currency, stooqCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    if (!fiatPrices.ContainsKey(stooqCurrency))
                    {
                        var fiatMetaData = await _fiatLogic.GetFiatByIdAsync(stooqCurrency, currency);
                        fiatPrices.Add(stooqCurrency, fiatMetaData.Price);
                    }

                    price *= fiatPrices[stooqCurrency];
                }

                result.Add(new AssetMetadata
                {
                    AssetId = id,
                    Name = id,
                    Price = price,
                    Currency = currency,
                    Symbol = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching stock data for {id}");
            }
        }

        return result;
    }

    private class StooqCsvRecord
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
    }
}
