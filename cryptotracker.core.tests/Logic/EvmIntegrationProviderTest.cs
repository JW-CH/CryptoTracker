using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using cryptotracker.core.Logic.Integrations;
using cryptotracker.core.Models;
using cryptotracker.database.Models;

namespace cryptotracker.core.tests.Logic;

[TestFixture]
public class EvmIntegrationProviderTest
{
    private const string Address = "0x1111111111111111111111111111111111111111";

    [Test]
    public async Task GetBalancesAsync_ConvertsWeiToNativeCoin()
    {
        var provider = Provider(CryptoTrackerIntegrationType.Ethereum, AddressResponse("1500000000000000000"));

        var result = (await provider.GetBalancesAsync(Source(Address))).Single();

        Assert.That(result.Symbol, Is.EqualTo("ETH"));
        Assert.That(result.Balance, Is.EqualTo(1.5m));
        Assert.That(result.AssetType, Is.EqualTo(AssetType.Crypto));
    }

    [Test]
    public async Task GetBalancesAsync_ZeroBalance_ReturnsNothing()
    {
        var provider = Provider(CryptoTrackerIntegrationType.Ethereum, AddressResponse("0"));

        var result = await provider.GetBalancesAsync(Source(Address));

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetBalancesAsync_Polygon_UsesPol()
    {
        var provider = Provider(CryptoTrackerIntegrationType.Polygon, AddressResponse("2000000000000000000"));

        var result = (await provider.GetBalancesAsync(Source(Address))).Single();

        Assert.That(result.Symbol, Is.EqualTo("POL"));
        Assert.That(result.Balance, Is.EqualTo(2m));
    }

    [Test]
    public void GetBalancesAsync_AddressEndpointFails_Throws()
    {
        var provider = Provider(CryptoTrackerIntegrationType.Base, null, fail: true);

        Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetBalancesAsync(Source(Address)));
    }

    [Test]
    public void GetBalancesAsync_MissingAddress_ThrowsBeforeHttp()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new EvmIntegrationProvider(CryptoTrackerIntegrationType.Ethereum, HttpTestHelpers.FactoryFor(handler));

        Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetBalancesAsync(Source("  ")));
        Assert.That(handler.Requests, Is.Empty);
    }

    private static EvmIntegrationProvider Provider(CryptoTrackerIntegrationType type, JsonObject? address, bool fail = false)
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (fail) return new HttpResponseMessage(HttpStatusCode.BadGateway);
            Assert.That(request.RequestUri!.ToString(), Does.Contain("blockscout.com"));
            Assert.That(request.RequestUri.ToString(), Does.Not.Contain("token-balances"));
            return Json(address!);
        });
        return new EvmIntegrationProvider(type, HttpTestHelpers.FactoryFor(handler));
    }

    private static CryptoTrackerIntegrationSource Source(string key) => new()
    {
        Type = CryptoTrackerIntegrationType.Ethereum,
        Key = key,
    };

    private static JsonObject AddressResponse(string wei) => new() { ["coin_balance"] = wei };

    private static HttpResponseMessage Json(JsonNode payload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
    };
}
