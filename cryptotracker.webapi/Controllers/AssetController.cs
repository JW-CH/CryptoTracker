using System.ComponentModel.DataAnnotations;
using cryptotracker.core.Logic;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static cryptotracker.webapi.Services.AssetService;

namespace cryptotracker.webapi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly AssetService _assetService;

        public AssetController(AssetService assetService)
        {
            _assetService = assetService;
        }

        [HttpGet(Name = "GetAssets")]
        public async Task<List<Asset>> GetAssets()
        {
            return await _assetService.GetAssetsAsync();
        }

        [HttpGet("{symbol}", Name = "GetAsset")]
        public async Task<AssetWithPriceDto> GetAsset([Required] string symbol)
        {
            return await _assetService.GetAssetWithPriceAsync(symbol);
        }

        [HttpGet("coin", Name = "GetCoins")]
        public async Task<List<Coin>> GetCoins()
        {
            return await _assetService.GetCoinsAsync();
        }

        [HttpGet("{symbol}/coin", Name = "FindCoinsBySymbol")]
        public async Task<List<Coin>> FindCoinsBySymbol([Required] string symbol)
        {
            return await _assetService.FindCoinsBySymbolAsync(symbol);
        }

        [HttpGet("fiat", Name = "GetFiats")]
        public async Task<List<Currency>> GetFiats()
        {
            return await _assetService.GetCurrenciesAsync();
        }

        [HttpGet("{symbol}/fiat", Name = "FindFiatBySymbol")]
        public async Task<List<Currency>> FindFiatBySymbol([Required] string symbol)
        {
            return await _assetService.FindCurrenciesBySymbolAsync(symbol);
        }

        [HttpPost("{symbol}/ExternalId", Name = "SetExternalIdForSymbol")]
        public async Task<AssetWithPriceDto> SetExternalIdForSymbol([Required] string symbol, [FromBody] string externalId)
        {
            return await _assetService.SetExternalIdAsync(symbol, externalId);
        }

        [HttpPost("{symbol}/Visibility", Name = "SetVisibilityForSymbol")]
        public async Task<bool> SetVisibilityForSymbol([Required] string symbol, [FromBody] bool isHidden)
        {
            await _assetService.SetVisibilityAsync(symbol, isHidden);
            return true;
        }

        [HttpPost("{symbol}/AssetType", Name = "SetAssetTypeForSymbol")]
        public async Task<bool> SetAssetTypeForSymbol([Required] string symbol, [FromBody] AssetType assetType)
        {
            await _assetService.SetAssetTypeAsync(symbol, assetType);
            return true;
        }

        [HttpPost(Name = "AddAsset")]
        public async Task<bool> AddAsset([FromBody] AddAssetDto assetDto)
        {
            await _assetService.AddAssetAsync(assetDto);
            return true;
        }

        [HttpDelete("{symbol}", Name = "DeleteAsset")]
        public async Task<bool> DeleteAsset([Required] string symbol)
        {
            await _assetService.DeleteAssetAsync(symbol);
            return true;
        }

        [HttpPost("Reset", Name = "ResetAsset")]
        public async Task<bool> ResetAsset([FromBody] string symbol)
        {
            await _assetService.ResetAssetAsync(symbol);
            return true;
        }
    }
}
