using System.Threading.Tasks;
using cryptotracker.core.Logic;
using cryptotracker.database.Models;
using cryptotracker.webapi.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using static cryptotracker.webapi.Controllers.AssetController;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class WebApiTest
{
    private DatabaseContext _dbContext;
    private AssetController _controller;
    private Mock<ILogger<CryptoTrackerController>> _loggerMock;
    private Mock<IFiatLogic> _fiatLogicMock;
    private Mock<ICryptoTrackerLogic> _cryptoTrackerLogicMock;
    private Mock<IStockLogic> _stockLogicMock;

    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB pro Test
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new DatabaseContext(options);
        _loggerMock = new Mock<ILogger<CryptoTrackerController>>();

        _fiatLogicMock = new Mock<IFiatLogic>();
        _cryptoTrackerLogicMock = new Mock<ICryptoTrackerLogic>();

        _stockLogicMock = new Mock<IStockLogic>();
        _stockLogicMock.Setup(x => x.GetAllStocksAsync()).ReturnsAsync(new List<Stock>() { new Stock { Symbol = "TST", Name = "Test Stock" } });
        _stockLogicMock.Setup(x => x.GetStocksByIdsAsync(It.IsAny<string>(), It.IsAny<List<string>>())).ReturnsAsync(new List<AssetMetadata>() { new AssetMetadata { Symbol = "TST", Name = "Test Stock" } });
        _stockLogicMock.Setup(x => x.GetStockByIdAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new AssetMetadata { Symbol = "TST", Name = "Test Stock" });

        _controller = new AssetController(
            _loggerMock.Object,
            _dbContext,
            _cryptoTrackerLogicMock.Object,
            _fiatLogicMock.Object,
            _stockLogicMock.Object
        );

        await SeedDatabase();
    }

    private async Task SeedDatabase()
    {
        _dbContext.Assets.Add(new Asset
        {
            Symbol = "BTC",
            Name = "Bitcoin",
            AssetType = AssetType.Crypto,
            ExternalId = "bitcoin",
            IsHidden = false
        });
        _dbContext.AssetPriceHistory.Add(new AssetPriceHistory
        {
            Symbol = "BTC",
            Date = DateOnly.FromDateTime(DateTime.Today),
            Currency = "CHF",
            Price = 50M
        });
        await _dbContext.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Database.EnsureDeleted();
        _dbContext?.Dispose();
    }

    [Test]
    public async Task GetAssets_WithOneAssetInDatabase_ReturnsOneAsset()
    {
        // Act
        var result = await _controller.GetAssets();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Symbol, Is.EqualTo("BTC"));
    }

    [Test]
    public async Task GetAssets_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        _dbContext.Assets.RemoveRange(_dbContext.Assets);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAssets();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAssets_WithHiddenAssets_ReturnsAllAssetsIncludingHidden()
    {
        // Arrange
        _dbContext.Assets.Add(new Asset
        {
            Symbol = "HIDDEN",
            Name = "Hidden Asset",
            AssetType = AssetType.Crypto,
            ExternalId = "hidden",
            IsHidden = true  // Hidden!
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAssets();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(x => x.Symbol == "HIDDEN"), Is.True);
    }

    [Test]
    public async Task AddAsset_WithValidCryptoAsset_AddsAssetSuccessfully()
    {
        // Arrange
        var dto = new AddAssetDto
        {
            Symbol = "ETH",
            AssetType = AssetType.Crypto,
            ExternalId = "ethereum"
        };

        _cryptoTrackerLogicMock.Setup(x => x.GetCoinData("CHF", It.Is<List<string>>(l => l.Contains("ethereum"))))
            .ReturnsAsync(new List<AssetMetadata>
            {
                new AssetMetadata
                {
                    AssetId = "ethereum",
                    Symbol = "ETH",
                    Name = "Ethereum",
                    Price= 111M,
                    Currency= "CHF",
                }
            });

        // Act
        var addResult = await _controller.AddAsset(dto);
        var ethAssetData = await _controller.GetAsset("ETH");
        var allAssets = await _controller.GetAssets();

        // Assert
        Assert.That(addResult, Is.True);
        Assert.That(allAssets.Count, Is.EqualTo(2));
        Assert.That(allAssets.Any(x => x.Symbol == "ETH"), Is.True);
        Assert.That(ethAssetData.Asset.Symbol, Is.EqualTo("ETH"));
        Assert.That(ethAssetData.Asset.ExternalId, Is.EqualTo("ethereum"));
        Assert.That(ethAssetData.Asset.AssetType, Is.EqualTo(AssetType.Crypto));
        Assert.That(ethAssetData.Price, Is.EqualTo(111M));
    }

    [Test]
    public async Task AddAsset_WithDuplicateSymbol_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new AddAssetDto
        {
            Symbol = "BTC", // Already exists
            AssetType = AssetType.Crypto,
            ExternalId = "bitcoin"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _controller.AddAsset(dto)
        );
    }

    [Test]
    public async Task GetAsset_WithExistingSymbol_ReturnsAssetWithPrice()
    {
        // Act
        var result = await _controller.GetAsset("BTC");

        // Assert
        Assert.That(result.Asset.Symbol, Is.EqualTo("BTC"));
        Assert.That(result.Asset.Name, Is.EqualTo("Bitcoin"));
        Assert.That(result.Asset.ExternalId, Is.EqualTo("bitcoin"));
        Assert.That(result.Price, Is.EqualTo(50M));
    }
    [Test]
    public async Task GetAsset_WithNullSymbol_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _controller.GetAsset(null)
        );
    }

    [Test]
    public async Task GetAsset_WithEmptySymbol_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _controller.GetAsset(string.Empty)
        );
    }

    [Test]
    public async Task GetAsset_WithNonExistingSymbol_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _controller.GetAsset("NONEXISTENT")
        );

        Assert.That(ex.Message, Is.EqualTo("Asset not found"));
    }

    [Test]
    public async Task AddAsset_WithStockAsset_AddsCorrectAssetType()
    {
        // Arrange
        var dto = new AddAssetDto
        {
            Symbol = "AAPL",
            AssetType = AssetType.Stock,
            ExternalId = "apple"
        };

        // Act
        await _controller.AddAsset(dto);
        var asset = await _dbContext.Assets.FirstOrDefaultAsync(x => x.Symbol == "AAPL");

        // Assert
        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.AssetType, Is.EqualTo(AssetType.Stock));
    }
}
