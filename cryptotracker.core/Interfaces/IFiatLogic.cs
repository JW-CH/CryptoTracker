using cryptotracker.core.Logic;

public interface IFiatLogic
{
    Task<AssetMetadata> GetFiatByIdAsync(string baseCurrency, string currency);
    Task<List<AssetMetadata>> GetFiatsByIdsAsync(string baseCurrency, List<string> currencies);
    Task<List<Fiat>> GetFiatList();
}
public struct Fiat
{
    public string Symbol { get; set; }
    public string Name { get; set; }
}