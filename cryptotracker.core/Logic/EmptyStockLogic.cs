using cryptotracker.core.Logic;
using Microsoft.Extensions.Logging;

public class EmptyStockLogic : IStockLogic
{
    private ILogger _logger;
    public EmptyStockLogic(ILogger logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<Stock>> GetAllStocksAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<AssetMetadata> GetStockByIdAsync(string currency, string id)
    {
        var results = await GetStocksByIdsAsync(currency, new List<string> { id });
        return results.FirstOrDefault();
    }

    public async Task<List<AssetMetadata>> GetStocksByIdsAsync(string currency, List<string> ids)
    {
        _logger.LogTrace($"GetStocksByIdsAsync: {string.Join(",", ids)}");
        _logger.LogWarning("EmptyStockLogic: GetStocksByIdsAsync is not implemented. Returning empty list.");
        return await Task.FromResult(new List<AssetMetadata>());
    }
}
