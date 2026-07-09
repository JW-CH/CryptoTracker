using cryptotracker.core.Logic;

public interface ICurrencyProvider
{
    Task<IEnumerable<Currency>> GetCurrenciesAsync();

    Task<AssetMetadata> GetLatestRateAsync(string baseCurrency, string symbol);
    Task<IEnumerable<AssetMetadata>> GetLatestRatesAsync(string baseCurrency, IEnumerable<string> symbols);
}

public struct Currency
{
    public string Symbol { get; set; }
    public string Name { get; set; }
}