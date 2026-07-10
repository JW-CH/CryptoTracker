using System.Net;
using cryptotracker.core.Logic.CurrencyPriceProviders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace cryptotracker.core.tests.Logic;

[TestFixture]
public class FrankfurterCurrencyPriceProviderTest
{
    private static readonly Dictionary<string, string> Currencies = new()
    {
        ["CHF"] = "Swiss Franc",
        ["EUR"] = "Euro",
        ["USD"] = "United States Dollar",
    };

    private static readonly Dictionary<string, decimal> RatesPerChf = new()
    {
        ["EUR"] = 1.0844m,
        ["USD"] = 1.2366m,
    };

    private FrankfurterCurrencyPriceProvider _provider;
    private FakeHttpMessageHandler _handler;

    [SetUp]
    public void Setup()
    {
        (_provider, _handler) = CreateProvider(RatesForRequest);
    }

    [TearDown]
    public void TearDown()
    {
        _handler?.Dispose();
    }

    [Test]
    public async Task GetQuotesAsync_ForeignCurrency_ReturnsValueOfOneUnitInBaseCurrency()
    {
        // frankfurter: 1 CHF = <rate> EUR, so 1 EUR must be worth 1/<rate> CHF
        var result = (await _provider.GetQuotesAsync("chf", new List<string> { "eur" })).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Symbol, Is.EqualTo("EUR"));
        Assert.That(result[0].Currency, Is.EqualTo("chf"));
        Assert.That(result[0].Price, Is.EqualTo(1m / RatesPerChf["EUR"]));
        Assert.That(result[0].Price, Is.LessThan(1m), "1 EUR is worth less than 1 CHF; an inverted rate would be > 1");
    }

    [Test]
    public async Task GetQuotesAsync_BaseCurrencyItself_ReturnsPriceOfOne()
    {
        var result = (await _provider.GetQuotesAsync("chf", new List<string> { "chf" })).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Price, Is.EqualTo(1m));
    }

    [Test]
    public async Task GetQuotesAsync_MixedCurrencies_ReturnsBaseAndConvertedPrices()
    {
        var result = (await _provider.GetQuotesAsync("chf", new List<string> { "chf", "eur", "usd" })).ToList();

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Single(x => x.Symbol == "chf").Price, Is.EqualTo(1m));
        Assert.That(result.Single(x => x.Symbol == "EUR").Price, Is.EqualTo(1m / RatesPerChf["EUR"]));
        Assert.That(result.Single(x => x.Symbol == "USD").Price, Is.EqualTo(1m / RatesPerChf["USD"]));
    }

    [Test]
    public async Task GetQuotesAsync_InvalidRate_IsSkipped()
    {
        var (provider, _) = CreateProvider(_ => new Dictionary<string, decimal> { ["EUR"] = 0m });

        var result = await provider.GetQuotesAsync("chf", new List<string> { "eur" });

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetQuotesAsync_ApiError_Throws()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.RequestUri!.ToString().Contains("/currencies")
                ? HttpTestHelpers.JsonResponse(Currencies)
                : new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") });
        var provider = new FrankfurterCurrencyPriceProvider(NullLogger.Instance, HttpTestHelpers.FactoryFor(handler), new MemoryCache(new MemoryCacheOptions()));

        Assert.ThrowsAsync<Exception>(() => provider.GetQuotesAsync("chf", new List<string> { "eur" }));
    }

    [Test]
    public async Task GetAssetsAsync_ReturnsCurrencyList()
    {
        var result = (await _provider.GetAssetsAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Single(x => x.Symbol == "EUR").Name, Is.EqualTo("Euro"));
        Assert.That(result.Single(x => x.Symbol == "EUR").ExternalId, Is.EqualTo("EUR"), "for fiat the symbol is the external id; the UI relies on it being set");
    }

    [Test]
    public async Task GetAssetsAsync_SecondCall_IsServedFromCache()
    {
        await _provider.GetAssetsAsync();
        await _provider.GetAssetsAsync();

        Assert.That(_handler.RequestCount("/currencies"), Is.EqualTo(1));
    }

    [Test]
    public async Task GetAssetsAsync_ApiError_ThrowsAndIsNotCached()
    {
        var failing = true;
        var handler = new FakeHttpMessageHandler(_ =>
            failing
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") }
                : HttpTestHelpers.JsonResponse(Currencies));
        var provider = new FrankfurterCurrencyPriceProvider(NullLogger.Instance, HttpTestHelpers.FactoryFor(handler), new MemoryCache(new MemoryCacheOptions()));

        Assert.ThrowsAsync<Exception>(() => provider.GetAssetsAsync());

        // the failure must not be cached: once the API recovers, the next call succeeds
        failing = false;
        var result = await provider.GetAssetsAsync();
        Assert.That(result, Is.Not.Empty);
    }

    private static (FrankfurterCurrencyPriceProvider Provider, FakeHttpMessageHandler Handler) CreateProvider(Func<HttpRequestMessage, Dictionary<string, decimal>> rates)
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();

            if (url.Contains("/currencies"))
                return HttpTestHelpers.JsonResponse(Currencies);

            if (url.Contains("/latest"))
                return HttpTestHelpers.JsonResponse(new { amount = 1.0, @base = "CHF", rates = rates(request) });

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var provider = new FrankfurterCurrencyPriceProvider(NullLogger.Instance, HttpTestHelpers.FactoryFor(handler), new MemoryCache(new MemoryCacheOptions()));

        return (provider, handler);
    }

    // mimics frankfurter: only the currencies from the symbols query parameter are returned
    private static Dictionary<string, decimal> RatesForRequest(HttpRequestMessage request)
    {
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
        var symbols = (query["symbols"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);

        return symbols
            .Select(s => s.ToUpperInvariant())
            .Where(RatesPerChf.ContainsKey)
            .ToDictionary(s => s, s => RatesPerChf[s]);
    }
}
