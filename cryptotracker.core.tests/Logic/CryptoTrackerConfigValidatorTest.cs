using cryptotracker.core.Models;

namespace cryptotracker.core.tests.Logic;

[TestFixture]
public class CryptoTrackerConfigValidatorTest
{
    private static CryptoTrackerIntegration Integration(string name, params CryptoTrackerIntegrationType[] types)
    {
        return new CryptoTrackerIntegration
        {
            Name = name,
            Sources = types.Select(t => new CryptoTrackerIntegrationSource { Type = t }).ToList()
        };
    }

    private static CryptoTrackerConfig ConfigWith(params CryptoTrackerIntegration[] integrations)
    {
        return new CryptoTrackerConfig { Integrations = integrations.ToList() };
    }

    [Test]
    public void Validate_ValidConfig_DoesNotThrow()
    {
        var config = ConfigWith(
            Integration("Coinbase", CryptoTrackerIntegrationType.Coinbase),
            Integration("Ledger", CryptoTrackerIntegrationType.Bitcoin, CryptoTrackerIntegrationType.Ethereum, CryptoTrackerIntegrationType.Ripple));

        Assert.DoesNotThrow(() => CryptoTrackerConfigValidator.Validate(config));
    }

    [Test]
    public void Validate_NoIntegrations_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => CryptoTrackerConfigValidator.Validate(new CryptoTrackerConfig()));
    }

    [Test]
    public void Validate_DuplicateNames_Throws()
    {
        // duplicate names used to silently merge into one integration whose
        // sources zero-marked each other's holdings
        var config = ConfigWith(
            Integration("Ledger", CryptoTrackerIntegrationType.Bitcoin),
            Integration("ledger", CryptoTrackerIntegrationType.Ethereum));

        var ex = Assert.Throws<InvalidOperationException>(() => CryptoTrackerConfigValidator.Validate(config));
        Assert.That(ex!.Message, Does.Contain("Ledger"));
        Assert.That(ex.Message, Does.Contain("multiple sources"));
    }

    [Test]
    public void Validate_IntegrationWithoutSources_ThrowsWithMigrationHint()
    {
        // the old flat format (type/key at integration level) binds to an
        // integration without sources; the error must point to the new format
        var config = ConfigWith(Integration("Coinbase"));

        var ex = Assert.Throws<InvalidOperationException>(() => CryptoTrackerConfigValidator.Validate(config));
        Assert.That(ex!.Message, Does.Contain("sources"));
    }

    [Test]
    public void Validate_SourceWithUnknownType_Throws()
    {
        var config = ConfigWith(Integration("Coinbase", CryptoTrackerIntegrationType.Unknown));

        Assert.Throws<InvalidOperationException>(() => CryptoTrackerConfigValidator.Validate(config));
    }

    [Test]
    public void Validate_IntegrationWithoutName_Throws()
    {
        var config = ConfigWith(Integration("", CryptoTrackerIntegrationType.Coinbase));

        Assert.Throws<InvalidOperationException>(() => CryptoTrackerConfigValidator.Validate(config));
    }
}
