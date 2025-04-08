using cryptotracker.core.Logic;

public interface IStockLogic
{
    Task<AssetMetadata> GetStockByIdAsync(string currency, string id);
    Task<List<AssetMetadata>> GetStocksByIdsAsync(string currency, List<string> ids);
    Task<IEnumerable<Stock>> GetAllStocksAsync();
}
public struct Stock
{
    public string Symbol { get; set; }
    public string Name { get; set; }
}