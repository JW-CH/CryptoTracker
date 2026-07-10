# Architektur

Stand 2026-07-10: A1–A6 sind umgesetzt (Kurzprotokoll unten); offen sind nur
noch die A4-Reste.

## Ist-Zustand (nach dem Umbau)

```
cryptotracker.web      SvelteKit-SPA (adapter-static), generierter API-Client (oazapfts)
cryptotracker.webapi   dünne Controller → Services (Asset, AssetMetadata, Integration,
                       Measuring, PortfolioQuery) → DTOs in webapi/Dtos;
                       UpdateService (BackgroundService); Config via IConfiguration
                       (YAML-Provider + CRYPTOTRACKER_*-Env-Overrides);
                       Rate-Limiting auf Auth-Endpoints
cryptotracker.core     IPriceProvider (CoinGecko, Frankfurter, YahooFinance; IMemoryCache)
                       und IIntegrationProvider (Bitpanda, Coinbase, Binance, Kucoin,
                       Crypto.com, Bitcoin, Ethereum, Ripple); Config-Modelle
cryptotracker.database DbContext, Entities, Migrations — keine DTOs mehr
```

## A4 — Offene Reste: Lifetimes & Ressourcen 🟡

- Die Exchange-SDK-Clients (CryptoClients.Net) werden in den
  `IIntegrationProvider`n weiterhin **pro Abruf von Hand erzeugt** statt über
  die vom Paket vorgesehene DI-Registrierung (`AddCoinbase()` etc.) — eigener
  Schritt, wenn gewünscht.
- `UpdateService` läuft im selben Prozess wie die API. Für Self-Hosting
  pragmatisch und inzwischen dokumentiert („nur 1 Replica", README). Sauberer
  wären: Migration hinter Startup-Flag/Job, Import mit DB-Lock
  (z. B. `pg_advisory_lock`).

## Erledigt (Kurzprotokoll)

| Punkt | Ergebnis |
|---|---|
| **A1** — Schichtenschnitt 🟠 | `ApiHelper` → `PortfolioQueryService`; `AssetService`/`IntegrationService`/`MeasuringService` (dünne Controller, typisierte Fehler); DTOs von database nach `webapi/Dtos`; keine rohen Entities mehr im API-Vertrag (`AssetDto`). 2026-07-09/10 |
| **A2** — Integration-Provider-Pattern 🟠 | `IIntegrationProvider` (`Type`, `GetBalancesAsync`) ×8 statt 100-Zeilen-Switch; `ICryptoTrackerLogic`/`CryptoTrackerLogic` gelöscht; toter Cardano-Zweig samt `CardanoSharp.Wallet` entfernt (Enum-Wert bleibt für alte Configs, fehlender Provider = pro Integration isolierter Fehler). 2026-07-10 |
| **A3** — Preis-Provider vereinheitlicht 🟠 | `IPriceProvider` (`Handles`, `GetAssetsAsync`, `GetQuotesAsync`) mit CoinGecko/Frankfurter/Yahoo; Dispatch per `Handles` im `AssetMetadataService` (fehlender Provider = Skip+Warning im Batch, Throw nur bei User-Aktion); `stockapi` als Enum, Yahoo konditional; `AlphaVantage`/`EmptyStockLogic`/`TwelveDataSharp` gelöscht; API-Breaking `Coin`/`Currency` → `ProviderAsset`. 2026-07-10 |
| **A4** — teilweise | Caches mit TTL ([Bug 9](01-kritische-bugs.md)); `IHttpClientFactory` in allen eigenen HTTP-Aufrufen (CoinGecko, Frankfurter, Bitpanda, BTC/ETH/XRP); `cryptotracker.worker` gelöscht; Replica-Hinweis im README. Reste siehe oben |
| **A5** — Konfiguration 🟡 | Eigener YAML-`ConfigurationProvider` → `IConfiguration`; `CRYPTOTRACKER_*`-Env-Overrides (`__` für Nesting); `CONFIG_PATH`; Datei optional; Key-Casing egal; `LoadFromYml/Json` gelöscht, YamlDotNet nach webapi. Bewusst ohne `IOptions<T>`/`ValidateOnStart` (kein Reload-Bedarf, Startup-Checks decken Kritisches ab). 2026-07-10 |
| **A6** — Naming & Routen 🟡 | `MessungDto` → `AssetHoldingDto`, `IntegrationShit` → `IntegrationAmount`, `ImmichFrame`-Namespace korrigiert; Routen: `POST /api/Asset/{symbol}/reset`, `GET|POST /api/Integration/{id}/measuring`, `DELETE /api/Measuring/{id}`. 2026-07-10 |
