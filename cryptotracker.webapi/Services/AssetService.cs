using cryptotracker.core.Interfaces;
using cryptotracker.core.Logic;
using cryptotracker.database.Models;
using Microsoft.EntityFrameworkCore;

namespace cryptotracker.webapi.Services
{
    public class AssetService
    {
        private readonly DatabaseContext _db;
        private readonly IEnumerable<IPriceProvider> _priceProviders;
        private IPriceProvider _currencyProvider => _priceProviders.First(p => p.Handles.Contains(AssetType.Fiat));
        private IPriceProvider _cryptoProvider => _priceProviders.First(p => p.Handles.Contains(AssetType.Crypto));
        private readonly ICryptoTrackerConfig _config;
        private readonly AssetMetadataService _assetMetadataService;

        public AssetService(DatabaseContext db, IEnumerable<IPriceProvider> priceProviders, ICryptoTrackerConfig config, AssetMetadataService assetMetadataService)
        {
            _db = db;
            _priceProviders = priceProviders;
            _config = config;
            _assetMetadataService = assetMetadataService;
        }

        public async Task<List<Asset>> GetAssetsAsync()
        {
            return await _db.Assets.ToListAsync();
        }

        public async Task<AssetWithPriceDto> GetAssetWithPriceAsync(string symbol)
        {
            var asset = await GetAssetOrThrowAsync(symbol);

            return new AssetWithPriceDto
            {
                Asset = asset,
                Price = await GetLatestPriceAsync(asset.Symbol)
            };
        }

        public async Task<List<ProviderAsset>> GetCoinsAsync()
        {
            return (await _cryptoProvider.GetAssetsAsync()).ToList();
        }

        public async Task<List<ProviderAsset>> FindCoinsBySymbolAsync(string symbol)
        {
            var coinList = await _cryptoProvider.GetAssetsAsync();

            return coinList.Where(x => x.Symbol.ToLower() == symbol.ToLower()).ToList();
        }

        public async Task<List<ProviderAsset>> GetCurrenciesAsync()
        {
            return (await _currencyProvider.GetAssetsAsync()).ToList();
        }

        public async Task<List<ProviderAsset>> FindCurrenciesBySymbolAsync(string symbol)
        {
            var currencyList = await _currencyProvider.GetAssetsAsync();

            return currencyList.Where(x => x.Symbol.ToLower() == symbol.ToLower()).ToList();
        }

        public async Task<AssetWithPriceDto> SetExternalIdAsync(string symbol, string externalId)
        {
            var asset = await GetAssetOrThrowAsync(symbol);

            using var tx = await _db.Database.BeginTransactionAsync();

            asset.ExternalId = externalId;
            await _db.SaveChangesAsync();

            var metadata = await _assetMetadataService.FetchMetadataAsync(asset.AssetType, externalId)
                ?? throw new InvalidOperationException($"Metadata not found for {asset.Symbol}");

            await _assetMetadataService.UpdateMetadataForAssetAsync(metadata);

            await tx.CommitAsync();

            return new AssetWithPriceDto
            {
                Asset = asset,
                Price = await GetLatestPriceAsync(asset.Symbol)
            };
        }

        public async Task SetVisibilityAsync(string symbol, bool isHidden)
        {
            var asset = await GetAssetOrThrowAsync(symbol);
            asset.IsHidden = isHidden;
            await _db.SaveChangesAsync();
        }

        public async Task SetAssetTypeAsync(string symbol, AssetType assetType)
        {
            var asset = await GetAssetOrThrowAsync(symbol);

            if (!string.IsNullOrEmpty(asset.ExternalId)) throw new InvalidOperationException("Asset already has an external id and cannot change its type");

            asset.AssetType = assetType;
            await _db.SaveChangesAsync();
        }

        public async Task AddAssetAsync(AddAssetDto assetDto)
        {
            if (await _db.Assets.AnyAsync(x => x.Symbol.ToLower() == assetDto.Symbol.ToLower())) throw new InvalidOperationException("Asset with this symbol already exists");

            using var tx = await _db.Database.BeginTransactionAsync();

            var asset = new Asset
            {
                Symbol = assetDto.Symbol,
                ExternalId = assetDto.ExternalId,
                AssetType = assetDto.AssetType,
                IsHidden = false
            };

            await _db.Assets.AddAsync(asset);
            await _db.SaveChangesAsync();

            var metadata = await _assetMetadataService.FetchMetadataAsync(assetDto.AssetType, assetDto.ExternalId)
                ?? throw new InvalidOperationException($"Metadata not found for {asset.Symbol}");

            await _assetMetadataService.UpdateMetadataForAssetAsync(metadata);

            await tx.CommitAsync();
        }

        public async Task DeleteAssetAsync(string symbol)
        {
            var asset = await GetAssetOrThrowAsync(symbol);

            if (await _db.AssetMeasurings.AnyAsync(x => x.Asset == asset))
                throw new InvalidOperationException("Asset has measurings and cannot be deleted");

            _db.AssetPriceHistory.RemoveRange(_db.AssetPriceHistory.Where(x => x.Asset == asset));
            _db.Assets.Remove(asset);
            await _db.SaveChangesAsync();
        }

        public async Task ResetAssetAsync(string symbol)
        {
            var asset = await GetAssetOrThrowAsync(symbol);

            _db.AssetPriceHistory.RemoveRange(_db.AssetPriceHistory.Where(x => x.Asset == asset));

            asset.ExternalId = "";
            asset.Name = "";
            asset.Image = "";
            await _db.SaveChangesAsync();
        }

        private async Task<Asset> GetAssetOrThrowAsync(string symbol)
        {
            return await _db.Assets.FirstOrDefaultAsync(x => x.Symbol == symbol) ?? throw new KeyNotFoundException("Asset not found");
        }

        private async Task<decimal> GetLatestPriceAsync(string symbol)
        {
            var latest = await _db.AssetPriceHistory
                .Where(x => x.Symbol == symbol && x.Currency == _config.BaseCurrency)
                .OrderByDescending(x => x.Date)
                .FirstOrDefaultAsync();

            return latest?.Price ?? 0;
        }

        public struct AddAssetDto
        {
            public string Symbol { get; set; }
            public AssetType AssetType { get; set; }
            public string ExternalId { get; set; }
        }

        public struct AssetWithPriceDto
        {
            public required Asset Asset { get; set; }
            public required decimal Price { get; set; }
        }
    }
}
