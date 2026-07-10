using System.Net;
using cryptotracker.core.Logic.CryptoPriceProviders;
using Microsoft.Extensions.Caching.Memory;

namespace cryptotracker.core.tests.Logic;

[TestFixture]
public class CoingeckoPriceProviderTest
{
    private static readonly object[] CoinList =
    {
        new { id = "bitcoin", symbol = "btc", name = "Bitcoin" },
        new { id = "ethereum", symbol = "eth", name = "Ethereum" },
    };

    private static readonly object[] Markets =
    {
        new { id = "bitcoin", symbol = "btc", name = "Bitcoin", image = "btc.png", current_price = 50000m },
    };

    private CoingeckoPriceProvider _provider;
    private FakeHttpMessageHandler _handler;

    [SetUp]
    public void Setup()
    {
        _handler = new FakeHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();

            if (url.Contains("/coins/list"))
                return HttpTestHelpers.JsonResponse(CoinList);

            if (url.Contains("/coins/markets"))
                return HttpTestHelpers.JsonResponse(Markets);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        _provider = new CoingeckoPriceProvider(HttpTestHelpers.FactoryFor(_handler), new MemoryCache(new MemoryCacheOptions()));
    }

    [TearDown]
    public void TearDown()
    {
        _handler?.Dispose();
    }

    [Test]
    public async Task GetAssetsAsync_MapsCoingeckoIdToExternalId()
    {
        var result = (await _provider.GetAssetsAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        var btc = result.Single(x => x.Symbol == "btc");
        Assert.That(btc.ExternalId, Is.EqualTo("bitcoin"));
        Assert.That(btc.Name, Is.EqualTo("Bitcoin"));
    }

    [Test]
    public async Task GetAssetsAsync_SecondCall_IsServedFromCache()
    {
        await _provider.GetAssetsAsync();
        await _provider.GetAssetsAsync();

        Assert.That(_handler.RequestCount("/coins/list"), Is.EqualTo(1));
    }

    [Test]
    public async Task GetAssetsAsync_ApiError_ThrowsAndIsNotCached()
    {
        var failing = true;
        var handler = new FakeHttpMessageHandler(_ =>
            failing
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") }
                : HttpTestHelpers.JsonResponse(CoinList));
        var provider = new CoingeckoPriceProvider(HttpTestHelpers.FactoryFor(handler), new MemoryCache(new MemoryCacheOptions()));

        Assert.ThrowsAsync<Exception>(() => provider.GetAssetsAsync());

        // the failure must not be cached: once the API recovers, the next call succeeds
        failing = false;
        var result = await provider.GetAssetsAsync();
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public async Task GetQuotesAsync_MapsFieldsAndPrice()
    {
        var result = (await _provider.GetQuotesAsync("chf", new List<string> { "bitcoin" })).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].AssetId, Is.EqualTo("bitcoin"));
        Assert.That(result[0].Symbol, Is.EqualTo("btc"));
        Assert.That(result[0].Currency, Is.EqualTo("chf"));
        Assert.That(result[0].Price, Is.EqualTo(50000m));
    }

    [Test]
    public void GetQuotesAsync_ApiError_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") });
        var provider = new CoingeckoPriceProvider(HttpTestHelpers.FactoryFor(handler), new MemoryCache(new MemoryCacheOptions()));

        Assert.ThrowsAsync<Exception>(() => provider.GetQuotesAsync("chf", new List<string> { "bitcoin" }));
    }
}
