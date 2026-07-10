using System.Net;
using System.Text;
using System.Text.Json;
using cryptotracker.core.Logic.CurrencyPriceProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

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

    [SetUp]
    public void Setup()
    {
        _provider = CreateProvider(RatesForRequest);
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
        var provider = CreateProvider(_ => new Dictionary<string, decimal> { ["EUR"] = 0m });

        var result = await provider.GetQuotesAsync("chf", new List<string> { "eur" });

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAssetsAsync_ReturnsCurrencyList()
    {
        var result = (await _provider.GetAssetsAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Single(x => x.Symbol == "EUR").Name, Is.EqualTo("Euro"));
        Assert.That(result.Single(x => x.Symbol == "EUR").ExternalId, Is.EqualTo("EUR"), "for fiat the symbol is the external id; the UI relies on it being set");
    }

    private static FrankfurterCurrencyPriceProvider CreateProvider(Func<HttpRequestMessage, Dictionary<string, decimal>> rates)
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();

            if (url.Contains("/currencies"))
                return JsonResponse(Currencies);

            if (url.Contains("/latest"))
                return JsonResponse(new { amount = 1.0, @base = "CHF", rates = rates(request) });

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false));

        return new FrankfurterCurrencyPriceProvider(NullLogger.Instance, factoryMock.Object);
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

    private static HttpResponseMessage JsonResponse(object payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
