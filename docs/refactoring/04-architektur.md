# Architektur

## Ist-Zustand

```
cryptotracker.web      SvelteKit-SPA (adapter-static), generierter API-Client
cryptotracker.webapi   Controller + ApiHelper + UpdateService (BackgroundService) + JwtService
cryptotracker.core     "Logic"-Klassen (Exchange-Abrufe, Preis-Provider), Config-Modelle
cryptotracker.database DbContext, Entities, Migrations — und DTOs (!)
```

## A1 — Schichtenschnitt: DTOs im database-Projekt, Logik in Controllern 🟠

> **Status 2026-07-09: teilweise umgesetzt.** `ApiHelper` → `PortfolioQueryService`
> und `AssetController` → `AssetService` (Controller ist jetzt dünn; Asset-Lookup,
> Preis-Lookup und Metadaten-Switch dedupliziert). Dabei Bug gefixt:
> `SetExternalIdForSymbol` behandelte Stocks wie Crypto (falscher Metadaten-Pfad).
> `CryptoTrackerAssetLogic` (core) → `AssetMetadataService` (webapi, scoped, DI):
> DbContext injiziert statt als Methodenparameter, keine Hand-Instanziierung mehr;
> Einzel- (`FetchMetadataAsync`) und Batch-Dispatch liegen jetzt in einer Klasse.
> Offen: DTOs liegen weiter im database-Projekt, Entities werden weiter roh
> serialisiert.

**Befund:**

- `MessungDto`, `AssetDto`, `IntegrationDto` liegen in `cryptotracker.database/DTOs/`.
  DTOs sind API-Verträge — sie gehören in die API-Schicht (oder ein eigenes
  Contracts-Projekt), nicht neben die Entities. Aktuell erzwingt das die
  Abhängigkeitsrichtung `webapi → database` für reine Serialisierungstypen und
  verleitet dazu, Entities direkt zu serialisieren — was `AssetController.GetAssets`
  auch tut (gibt das EF-Entity `Asset` roh zurück, inkl. aller künftigen Spalten).
- Fachlogik lebt verteilt: Aggregation in `webapi/Helpers/ApiHelper` (statische
  Klasse), Metadaten-Update in `core/CryptoTrackerAssetLogic`, Import-Orchestrierung
  im `UpdateService`, Rest in Controllern (`AssetController.AddAsset` macht
  Transaktion + Provider-Auswahl + Metadaten-Update selbst).
- `CryptoTrackerAssetLogic` wird an zwei Stellen **per Hand instanziiert**
  (`AssetController.cs:29`, `UpdateService.cs:37`) statt über DI registriert.

**Empfehlung:** Drei klare Schichten:

```
Controllers (dünn: Binding, Statuscodes)
   ↓
Services (PortfolioQueryService, AssetService, IntegrationService, ImportService)
   ↓
Infrastruktur (DbContext, IIntegrationProvider, IPriceProvider)
```

- `ApiHelper` → `PortfolioQueryService` (injizierbar, testbar, kein `static`).
- DTOs + Mapping in die webapi (oder `cryptotracker.contracts`), Entities bleiben intern.
- Alles über DI registrieren; keine `new`-Aufrufe von Services in Controllern.

## A2 — Der Integration-`switch`: Provider-Pattern 🟠

`CryptoTrackerLogic.GetAvailableIntegrationBalances` ist ein 100-Zeilen-`switch`
über String-Typen mit je eigener Client-Erzeugung, plus ~300 Zeilen private
Abruf-Methoden — dazu CoinGecko-Zugriff in derselben Klasse. Die Klasse hat
mindestens drei Verantwortlichkeiten (Exchange-Balances, Blockchain-Balances,
Coin-Metadaten) und ist der Grund, warum `core` acht Exchange-SDKs referenziert.

**Empfehlung:**

```csharp
public interface IIntegrationProvider
{
    string Type { get; }                       // "coinbase", "bitcoin", …
    Task<IntegrationFetchResult> GetBalancesAsync(
        IntegrationCredentials creds, CancellationToken ct);
}
```

- Eine Klasse pro Provider (`CoinbaseProvider`, `BitcoinXpubProvider`, …),
  Registrierung als `IEnumerable<IIntegrationProvider>` + Lookup per `Type`.
- `IntegrationFetchResult` unterscheidet Erfolg/Fehler explizit
  (fixt [Bug 5](01-kritische-bugs.md#bug-5); leere Liste ≠ Fehler).
- Neue Exchanges = neue Datei, kein Anfassen des Kerns; Provider einzeln testbar.
- CoinGecko/Metadaten in einen eigenen `ICryptoMetadataProvider` ausgliedern.

**Hinterfragt — `NotImplementedException`-Pfade:** Der Cardano-Zweig wirft
in einer lokalen Funktion `NotImplementedException` mit unerreichbarem Code
dahinter (`CryptoTrackerLogic.cs:146–166`) — eine konfigurierte Cardano-
Integration bricht damit den gesamten Import-Lauf jeder Runde aufs Neue
(alles in einer Transaktion, siehe Bug 5). Toten Zweig entfernen oder fertig
bauen; halbfertige Features nicht in den Dispatch hängen.

## A3 — Preis-Provider: drei Interfaces, drei Semantiken 🟠

<a id="a3"></a>

> **Status 2026-07-10: umgesetzt.** Ein Interface `IPriceProvider`
> (`Handles: IEnumerable<AssetType>`, `GetAssetsAsync` → `ProviderAsset`,
> `GetQuotesAsync` → `AssetMetadata`) mit drei Implementierungen:
> `CoingeckoPriceProvider`, `FrankfurterCurrencyPriceProvider` (dient Yahoo
> zugleich als FX-Quelle) und `YahooFinancePriceProvider`. Registrierung als
> `IEnumerable<IPriceProvider>`, Dispatch per `Handles` im `AssetMetadataService`;
> fehlender Provider ist im Batch-Lauf ein Skip + Warning, nur bei expliziter
> User-Aktion (`FetchMetadataAsync`) ein Fehler. `ICurrencyProvider`/`IStockLogic`
> gelöscht, `ICryptoTrackerLogic` enthält nur noch Exchange-Balances (Vorarbeit
> für A2). `stockapi` ist jetzt ein Enum (`yahoofinance`), Yahoo wird nur
> konditional registriert; `AlphaVantageStockLogic`/`EmptyStockLogic` gelöscht
> (Package-Referenzen `ThreeFourteen.AlphaVantage`/`TwelveDataSharp` können noch
> aus dem csproj raus). API-Breaking umgesetzt: `Coin`/`Currency` →
> `ProviderAsset` im generierten Client (`externalId` statt `id`).

`ICryptoTrackerLogic` (Crypto via CoinGecko), `IFiatLogic` (Frankfurter),
`IStockLogic` (Yahoo/AlphaVantage/Empty) haben fast identische Aufgaben, aber
unterschiedliche Signaturen und — kritisch — unterschiedliche
Preis-Semantik (daher [Bug 1](01-kritische-bugs.md#bug-1)).

**Empfehlung:** Ein Interface, dispatcht nach `AssetType`:

```csharp
public interface IPriceProvider
{
    AssetType Handles { get; }
    Task<IReadOnlyList<AssetQuote>> GetQuotesAsync(
        string baseCurrency, IReadOnlyList<string> externalIds, CancellationToken ct);
}
// AssetQuote.Price ≡ Wert von 1 Einheit in baseCurrency — dokumentiert und getestet.
```

Auswahl-Logik (`StockApi`-Config als Quasi-Boolean, `Program.cs:66–80`:
`stockapi: test` aktiviert Yahoo, das den Wert ignoriert; `AlphaVantageStockLogic`
ist toter Code, `TwelveDataSharp` ein ungenutztes Package) durch explizite
Konfiguration ersetzen: `stocks.provider: yahoo|alphavantage|none` +
`stocks.apiKey`.

## A4 — Lifetimes und Ressourcen 🟡

- `ICryptoTrackerLogic`/`IFiatLogic`/`IStockLogic` sind **Singletons** mit
  internem mutablen Zustand (Caches, [Bug 9](01-kritische-bugs.md)) — ohne
  Synchronisierung. Entweder Scoped + externer `IMemoryCache`, oder Singleton
  mit sauberem Lazy-Cache.
- `CryptoTrackerLogic` erzeugt pro Aufruf `new HttpClient()` bzw. neue
  Exchange-REST-Clients — Socket-Exhaustion-Risiko und keine gemeinsame
  Pipeline (Timeouts, Retry). `IHttpClientFactory` ist registriert
  (`Program.cs:47`) und wird von `FiatLogic` korrekt genutzt — nur konsequent
  überall verwenden; für Exchange-SDKs die von CryptoClients.Net vorgesehene
  DI-Registrierung (`AddCoinbase()` etc.) prüfen.
- `UpdateService` läuft **im selben Prozess wie die API**. Das ist für
  Self-Hosting pragmatisch und darf so bleiben — aber: bei mehreren Replicas
  importiert jede Instanz doppelt, und `db.Database.Migrate()` beim Start
  (`Program.cs:225`) racet ebenfalls. Mindestens dokumentieren („nur 1 Replica"),
  besser: Migration hinter Startup-Flag/Job, Import mit DB-Lock (z. B.
  `pg_advisory_lock`).
- ~~`cryptotracker.worker` ist als legacy markiert (CLAUDE.md), enthält eine
  Kopie der Import-Logik und veraltet parallel. **Löschen** (Git-Historie
  bewahrt ihn), ebenso `SimpleHttpClientFactory`.~~ ✅ erledigt 2026-07-09.

## A5 — Konfiguration 🟡

- Eigenes Config-System (YAML/JSON-Datei, selbst geladen in `Program.cs:250`)
  statt `IConfiguration`. Damit funktionieren weder Env-Var-Overrides noch
  User-Secrets noch `appsettings.{Environment}.json` — im Docker-Umfeld
  schmerzhaft (Secrets nur per Datei). Empfehlung: YAML-Datei als
  Konfigurationsquelle in `IConfiguration` einhängen
  (`AddYamlFile` via Paket oder eigener Provider) + `AddEnvironmentVariables()`;
  typisiert per `IOptions<CryptoTrackerOptions>` mit Validierung
  (`ValidateDataAnnotations`, `ValidateOnStart`).
- Pfad-Logik `../config` vs. `./config` je nach Environment ist fragil —
  expliziter `CONFIG_PATH`-Env-Var mit Default wäre robuster.
- `LowerCaseNamingConvention` zwingt Config-Keys zu `connectionstring` — die
  bei .NET üblichen `camelCase`/`PascalCase`-Schreibweisen scheitern still.

## A6 — Naming & Konventionen (Sammelpunkt) 🟡

Details in [05](05-backend-codequalitaet.md), aber architekturrelevant:

- Deutsch/Englisch gemischt bis in die API: `MessungDto` ist Teil des generierten
  TypeScript-Clients. Vor dem API-Freeze auf Englisch vereinheitlichen
  (`AssetHoldingDto`), sonst zieht sich das für immer durch.
- `IntegrationShit` (`AssetMeasuringDto.cs:78`) — offensichtlich ein Platzhalter,
  der es in den öffentlichen API-Vertrag geschafft hat → `IntegrationAmount`.
- Namespace `ImmichFrame.Core.Helpers` (`HttpClientExtensionMethods.cs:1`) ist
  aus einem anderen Projekt kopiert — Namespace korrigieren.
- Route-Design uneinheitlich: `POST /api/Measuring?id=…` bindet die Integrations-Id
  aus dem Query-String (`MeasuringController.cs:35`), während sonst Route-Parameter
  üblich sind (`/api/Integration/{id}/measuring` wäre konsistent). REST-Verben:
  `POST …/Visibility` mit bool-Body ist ok, aber `Reset` via `POST /api/Asset/Reset`
  mit Symbol im Body bricht das Muster (`/api/Asset/{symbol}/reset`).
