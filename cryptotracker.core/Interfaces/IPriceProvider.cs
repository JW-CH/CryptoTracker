

using cryptotracker.core.Logic;
using cryptotracker.database.Models;

namespace cryptotracker.core.Interfaces;

public interface IPriceProvider
{
    IEnumerable<AssetType> Handles { get; }
    Task<IEnumerable<ProviderAsset>> GetAssetsAsync();
    Task<IEnumerable<AssetMetadata>> GetQuotesAsync(string baseCurrency, IEnumerable<string> externalIds);
}

public struct AssetMetadata
{
    public string AssetId { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Image { get; set; }
    public string Currency { get; set; }
    /// <summary>
    /// Value of 1 unit of the asset, expressed in <see cref="Currency"/>.
    /// All providers (crypto, fiat, stock) must follow this semantic.
    /// </summary>
    public decimal Price { get; set; }
}

public struct ProviderAsset
{
    public string ExternalId { get; set; }
    public string Symbol { get; set; }
    public string Name { get; set; }
}