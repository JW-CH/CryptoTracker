using Coinbase.Net.Clients;
using CryptoExchange.Net.Authentication;
using cryptotracker.core.Logic;
using cryptotracker.core.Models;
using cryptotracker.database.Models;
using ImmichFrame.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace cryptotracker.webapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CryptoTrackerController : ControllerBase
    {
        private readonly ILogger<CryptoTrackerController> _logger;
        private readonly ApplicationContext _db;
        private readonly CryptotrackerConfig _config;

        public CryptoTrackerController(ILogger<CryptoTrackerController> logger, ApplicationContext db, CryptotrackerConfig config)
        {
            _logger = logger;
            _db = db;
            _config = config;
        }

        [HttpGet(Name = "Get")]
        public async Task<string> Get()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var integration in _config.Integrations)
            {
                switch (integration.Type.ToLower())
                {
                    case "bitpanda":
                        using (var client = new HttpClient())
                        {
                            client.UseApiKey(integration.Secret);
                            var accounts2 = await CryptoTrackerLogic.GetBitpandaAccounts(client);

                            foreach (var account in accounts2)
                            {
                                AddMeasuring(integration, account.Attributes.CryptocoinSymbol, account.Attributes.Name, Convert.ToDecimal(account.Attributes.Balance));

                                sb.AppendLine($"{account.Attributes.Name}: {account.Attributes.Balance}");
                            }
                        }

                        break;
                    case "coinbase":
                        using (var cbClient = new CoinbaseRestClient(xy =>
                        {
                            xy.ApiCredentials = new ApiCredentials(integration.Key, integration.Secret);
                        }))
                        {
                            var accounts = await CryptoTrackerLogic.GetCoinbaseAvailableAccounts(cbClient);
                            foreach (var account in accounts)
                            {
                                AddMeasuring(integration, account.Asset, account.Name, account.AvailableBalance.Value);

                                sb.AppendLine($"{account.Name}: {account.AvailableBalance.Value}");
                            }
                        }

                        break;
                    default:
                        throw new NotImplementedException($"Integration {integration.Type} was not implemented!");
                }

            }

            return sb.ToString();
        }

        private void AddMeasuring(CryptotrackerIntegration integration, string symbol, string assetName, decimal value)
        {
            var ex = _db.ExchangeIntegrations.FirstOrDefault(x => x.Name.ToLower() == integration.Name.ToLower());

            if (ex == null)
            {
                ex = new ExchangeIntegration()
                {
                    Name = integration.Name,
                    Description = integration.Description
                };
                _db.ExchangeIntegrations.Add(ex);
            }

            var asset = _db.Assets.Find(symbol);

            if (asset == null)
            {
                asset = new Asset()
                {
                    AssetId = symbol,
                    Name = assetName,
                    Description = ""
                };
                _db.Assets.Add(asset);
            }

            var x = new AssetMeasuring()
            {
                Asset = asset,
                Integration = ex,
                StandingDate = DateTime.Now,
                StandingValue = value
            };

            _db.AssetMeasurings.Add(x);
            _db.SaveChanges();
        }
    }
}
