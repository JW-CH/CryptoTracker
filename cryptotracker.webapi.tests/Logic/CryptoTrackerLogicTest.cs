using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace cryptotracker.core.tests.Logic;

[TestFixture]
public class CryptoTrackerLogicTest
{
    private Mock<ILogger> _mockLogger;
    private CryptoTrackerLogic _logic;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _logic = new CryptoTrackerLogic(_mockLogger.Object);
    }

    [Test]
    public void GetAvailableIntegrationBalances_WithInvalidType_ThrowsNotImplementedException()
    {
        // Arrange
        var integration = new CryptoTrackerIntegration
        {
            Type = "invalid",
            Name = "Test"
        };

        // Act & Assert
        Assert.ThrowsAsync<NotImplementedException>(
            async () => await _logic.GetAvailableIntegrationBalances(integration)
        );
    }
}