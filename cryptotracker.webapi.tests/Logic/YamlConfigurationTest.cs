using cryptotracker.core.Interfaces;
using cryptotracker.core.Models;
using cryptotracker.webapi.Configuration;
using Microsoft.Extensions.Configuration;

namespace cryptotracker.webapi.tests.Logic;

[TestFixture]
public class YamlConfigurationTest
{
    private string _configPath;

    [SetUp]
    public void Setup()
    {
        _configPath = Path.Combine(Path.GetTempPath(), $"cryptotracker-test-{Guid.NewGuid()}.yml");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_configPath)) File.Delete(_configPath);
    }

    private CryptoTrackerConfig LoadConfig(string yaml, Dictionary<string, string?>? overrides = null)
    {
        File.WriteAllText(_configPath, yaml);

        var builder = new ConfigurationBuilder().AddYamlFile(_configPath);
        if (overrides != null) builder.AddInMemoryCollection(overrides);

        return builder.Build().Get<CryptoTrackerConfig>() ?? new CryptoTrackerConfig();
    }

    [Test]
    public void Load_FullConfig_BindsAllSectionsAndEnums()
    {
        var config = LoadConfig("""
            connectionstring: Host=localhost;Database=test
            interval: 30
            loglevel: debug
            maxfilldays: 5
            basecurrency: EUR
            stockapi: yahoofinance
            auth:
              secret: some-secret
            integrations:
              - name: My Coinbase
                type: coinbase
                key: k
                secret: s
              - name: Wallet
                type: bitcoin
                key: zpub123
            """);

        Assert.That(config.ConnectionString, Is.EqualTo("Host=localhost;Database=test"));
        Assert.That(config.Interval, Is.EqualTo(30));
        Assert.That(config.MaxFillDays, Is.EqualTo(5));
        Assert.That(config.BaseCurrency, Is.EqualTo("eur"), "setter normalizes to lowercase");
        Assert.That(config.StockApi, Is.EqualTo(StockApi.YahooFinance));
        Assert.That(config.Auth.Secret, Is.EqualTo("some-secret"));
        Assert.That(config.Integrations, Has.Count.EqualTo(2));
        Assert.That(config.Integrations[0].Type, Is.EqualTo(CryptoTrackerIntegrationType.Coinbase));
        Assert.That(config.Integrations[1].Type, Is.EqualTo(CryptoTrackerIntegrationType.Bitcoin));
        Assert.That(config.Integrations[1].Key, Is.EqualTo("zpub123"));
    }

    [Test]
    public void Load_MixedCasingKeys_BindsCaseInsensitively()
    {
        var config = LoadConfig("""
            connectionString: cs
            MaxFillDays: 7
            """);

        Assert.That(config.ConnectionString, Is.EqualTo("cs"));
        Assert.That(config.MaxFillDays, Is.EqualTo(7));
    }

    [Test]
    public void Load_LaterSourceOverridesYaml()
    {
        // mirrors the env var override (CRYPTOTRACKER_AUTH__SECRET etc.)
        var config = LoadConfig("""
            interval: 30
            auth:
              secret: from-file
            """,
            new Dictionary<string, string?> { ["auth:secret"] = "from-env" });

        Assert.That(config.Auth.Secret, Is.EqualTo("from-env"));
        Assert.That(config.Interval, Is.EqualTo(30), "keys without override keep the file value");
    }

    [Test]
    public void Load_MissingOptionalFile_YieldsDefaults()
    {
        var builder = new ConfigurationBuilder().AddYamlFile(Path.Combine(Path.GetTempPath(), "does-not-exist.yml"), optional: true);

        var config = builder.Build().Get<CryptoTrackerConfig>() ?? new CryptoTrackerConfig();

        Assert.That(config.BaseCurrency, Is.EqualTo("chf"));
        Assert.That(config.Interval, Is.EqualTo(60));
    }

    [Test]
    public void Load_MissingRequiredFile_Throws()
    {
        var builder = new ConfigurationBuilder().AddYamlFile(Path.Combine(Path.GetTempPath(), "does-not-exist.yml"));

        Assert.Throws<FileNotFoundException>(() => builder.Build());
    }
}
