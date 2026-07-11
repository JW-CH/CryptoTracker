using System.Threading.Tasks;
using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using cryptotracker.webapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using static cryptotracker.webapi.Services.AssetService;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class AssetServiceTest
{
    private DatabaseContext _dbContext;
    private AssetService _service;
    private Mock<IPriceProvider> _cryptoProviderMock;
    private Mock<IPriceProvider> _currencyProviderMock;
    private Mock<IPriceProvider> _stockProviderMock;

    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB pro Test
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new DatabaseContext(options);

        _cryptoProviderMock = new Mock<IPriceProvider>();
        _cryptoProviderMock.Setup(x => x.Handles).Returns(new[] { AssetType.Crypto });

        _currencyProviderMock = new Mock<IPriceProvider>();
        _currencyProviderMock.Setup(x => x.Handles).Returns(new[] { AssetType.Fiat });

        _stockProviderMock = new Mock<IPriceProvider>();
        _stockProviderMock.Setup(x => x.Handles).Returns(new[] { AssetType.Stock });
        _stockProviderMock.Setup(x => x.GetQuotesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((string currency, IEnumerable<string> ids) => new List<AssetMetadata> { new AssetMetadata { AssetId = ids.First(), Symbol = "TST", Name = "Test Stock" } });

        var priceProviders = new[] { _cryptoProviderMock.Object, _currencyProviderMock.Object, _stockProviderMock.Object };

        var config = new CryptoTrackerConfig();
        var metadataService = new AssetMetadataService(
            Mock.Of<ILogger<AssetMetadataService>>(),
            _dbContext,
            priceProviders,
            config,
            TestClock.Create()
        );

        _service = new AssetService(
            _dbContext,
            priceProviders,
            config,
            metadataService
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
            Currency = "chf",
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
        var result = await _service.GetAssetsAsync();

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
        var result = await _service.GetAssetsAsync();

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
        var result = await _service.GetAssetsAsync();

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

        _cryptoProviderMock.Setup(x => x.GetQuotesAsync("chf", It.Is<IEnumerable<string>>(l => l.Contains("ethereum"))))
            .ReturnsAsync(new List<AssetMetadata>
            {
                new AssetMetadata
                {
                    AssetId = "ethereum",
                    Symbol = "ETH",
                    Name = "Ethereum",
                    Price= 111M,
                    Currency= "chf",
                }
            });

        // Act
        await _service.AddAssetAsync(dto);
        var ethAssetData = await _service.GetAssetWithPriceAsync("ETH");
        var allAssets = await _service.GetAssetsAsync();

        // Assert
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
            async () => await _service.AddAssetAsync(dto)
        );
    }

    [Test]
    public async Task AddAsset_WithUnknownCryptoExternalId_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new AddAssetDto
        {
            Symbol = "XXX",
            AssetType = AssetType.Crypto,
            ExternalId = "does-not-exist"
        };

        _cryptoProviderMock.Setup(x => x.GetQuotesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<AssetMetadata>());

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.AddAssetAsync(dto)
        );

        Assert.That(ex.Message, Does.Contain("Metadata not found"));
    }

    [Test]
    public async Task GetAsset_WithExistingSymbol_ReturnsAssetWithPrice()
    {
        // Act
        var result = await _service.GetAssetWithPriceAsync("BTC");

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
            async () => await _service.GetAssetWithPriceAsync(null)
        );
    }

    [Test]
    public async Task GetAsset_WithEmptySymbol_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.GetAssetWithPriceAsync(string.Empty)
        );
    }

    [Test]
    public async Task GetAsset_WithNonExistingSymbol_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.GetAssetWithPriceAsync("NONEXISTENT")
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
        await _service.AddAssetAsync(dto);
        var asset = await _dbContext.Assets.FirstOrDefaultAsync(x => x.Symbol == "AAPL");

        // Assert
        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.AssetType, Is.EqualTo(AssetType.Stock));
    }

    [Test]
    public async Task SetExternalId_WithStockAsset_UsesStockProviderAndWritesPrice()
    {
        // Arrange
        _dbContext.Assets.Add(new Asset
        {
            Symbol = "AAPL",
            Name = "",
            AssetType = AssetType.Stock,
            ExternalId = "",
            IsHidden = false
        });
        await _dbContext.SaveChangesAsync();

        _stockProviderMock.Setup(x => x.GetQuotesAsync("chf", It.Is<IEnumerable<string>>(l => l.Contains("apple"))))
            .ReturnsAsync(new List<AssetMetadata>
            {
                new AssetMetadata
                {
                    AssetId = "apple",
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Price = 42M,
                    Currency = "chf"
                }
            });

        // Act
        var result = await _service.SetExternalIdAsync("AAPL", "apple");

        // Assert
        Assert.That(result.Asset.ExternalId, Is.EqualTo("apple"));
        Assert.That(result.Price, Is.EqualTo(42M));

        var priceEntry = await _dbContext.AssetPriceHistory.FirstOrDefaultAsync(x => x.Symbol == "AAPL");
        Assert.That(priceEntry, Is.Not.Null);
        Assert.That(priceEntry.Price, Is.EqualTo(42M));

        // Stocks dürfen nicht über den Crypto-Pfad aufgelöst werden
        _stockProviderMock.Verify(x => x.GetQuotesAsync("chf", It.Is<IEnumerable<string>>(l => l.Contains("apple"))), Times.Once);
        _cryptoProviderMock.Verify(x => x.GetQuotesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Test]
    public async Task SetExternalId_WithUnknownCryptoExternalId_ThrowsInvalidOperationException()
    {
        // Arrange
        _cryptoProviderMock.Setup(x => x.GetQuotesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<AssetMetadata>());

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.SetExternalIdAsync("BTC", "does-not-exist")
        );

        Assert.That(ex.Message, Does.Contain("Metadata not found"));
    }

    [Test]
    public async Task SetExternalId_WithMetadataWithoutAssetId_ThrowsInvalidOperationException()
    {
        // Arrange (Provider liefert Metadaten ohne AssetId)
        _dbContext.Assets.Add(new Asset
        {
            Symbol = "AAPL",
            Name = "",
            AssetType = AssetType.Stock,
            ExternalId = "",
            IsHidden = false
        });
        await _dbContext.SaveChangesAsync();

        _stockProviderMock.Setup(x => x.GetQuotesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<AssetMetadata> { new AssetMetadata { Symbol = "AAPL", Name = "Apple Inc." } });

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.SetExternalIdAsync("AAPL", "apple")
        );
    }

    [Test]
    public async Task SetAssetType_WithExistingExternalId_ThrowsInvalidOperationException()
    {
        // Act & Assert (BTC hat ExternalId "bitcoin")
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.SetAssetTypeAsync("BTC", AssetType.Fiat)
        );
    }
}
