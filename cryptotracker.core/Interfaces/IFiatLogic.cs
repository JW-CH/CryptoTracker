using cryptotracker.core.Logic;

public interface IFiatLogic
{
    Task<AssetMetadata> GetFiatByIdAsync(string currency, string id);
    Task<List<AssetMetadata>> GetFiatsByIdsAsync(string currency, List<string> ids);
    Task<List<Fiat>> GetFiatList();
}
public struct Fiat
{
    public string Symbol { get; set; }
    public string Name { get; set; }
}