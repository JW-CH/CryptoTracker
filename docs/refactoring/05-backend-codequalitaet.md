# Backend — Code-Qualität

Ergänzend zu den strukturellen Punkten in [04-architektur.md](04-architektur.md);
hier die handwerklichen Themen, jeweils mit Fundstelle.

<a id="fehlerbehandlung"></a>
## Q1 — Fehlerbehandlung: generische Exceptions als Kontrollfluss 🟠

Controller werfen durchgängig `throw new Exception("Asset not found")`
(`AssetController.cs:83,125,135,201,216`, `MeasuringController.cs:39,41,45`,
`IntegrationController.cs:51` — dort sogar mit deutschem Text im API-Fehler).
Ergebnis: alles wird zum 500er, der Client kann Fehlerarten nicht unterscheiden,
und Swagger dokumentiert nur 200.

**Empfehlung:**

- Exception-Middleware + `ProblemDetails` (`AddProblemDetails()` ist in .NET 10
  Standardkost); fachliche Fehler als typisierte Exceptions (`NotFoundException`
  → 404, `ValidationException` → 400) oder Result-Pattern im Service-Layer.
- Rückgabetypen ehrlich machen: `Task<bool>`, das nie `false` zurückgibt
  (`AddAsset`, `DeleteAsset`, `SetVisibilityForSymbol`, …) → `ActionResult` mit
  201/204/404. `GetIntegrationDetails` gibt bei unbekannter Id `null` → 200 mit
  leerem Body statt 404 (`IntegrationController.cs:39`).

## Q2 — Async/EF-Hygiene 🟠

- `IntegrationController.GetIntegrations` und `MeasuringController.
  GetMeasuringsByIntegration` sind synchron und materialisieren mit
  LINQ-to-Objects (`.Select(IntegrationDto.FromModel)` auf dem DbSet zieht die
  ganze Tabelle client-seitig). → `await …
  .Select(x => new IntegrationDto{…}).ToListAsync()`.
- Keine einzige Query nutzt `AsNoTracking()`; die Aggregation trackt tausende
  Entities (siehe [03/D3](03-datenmodell-und-aggregation.md)).
- **Kein `CancellationToken` im gesamten Backend** — weder in Controllern noch
  in `UpdateService.Import` (der Token wird bei `ExecuteAsync` entgegengenommen
  und dann ignoriert bis zum `WaitForNextTickAsync`). Bei langsamen Exchange-APIs
  blockiert der Shutdown.
- `db.AssetMeasurings.AddAsync(...)`: `AddAsync` ist nur für ValueGenerators
  nötig; `Add` reicht — Mikropunkt, aber überall.
- `UpdateService.AddMeasuring` sucht die Integration **pro Balance** erneut in
  der DB (`FirstOrDefaultAsync` je Aufruf in einer Schleife über alle Balances,
  `UpdateService.cs:102`) — einmal vor der Schleife auflösen.
- `Program.cs:93`: `LogTo(Console.WriteLine)` umgeht das Logging-Framework
  (doppelte Ausgabe zu `AddFilter("Microsoft.EntityFrameworkCore", Warning)`).

## Q3 — HTTP-Aufrufe: keine Timeouts, kein Retry, kein Rate-Limit-Umgang 🟠

- CoinGecko free tier: ~5–15 req/min. `GetCoinList` (unpaginiert, ~2 MB JSON)
  und `GetCoinData` werden ohne Backoff aufgerufen; bei 429 wird nur geloggt
  und leer zurückgegeben. Der `UpdateService` läuft dann mit leeren Metadaten
  weiter — Preise des Tages fehlen einfach.
- `GetCoinData` baut **alle** externen Ids in eine URL (`CryptoTrackerLogic.cs:438`)
  — bei vielen Assets drohen URL-Längen-Limits und CoinGecko paginiert die
  Antwort (per_page Default 100) — mehr als 100 Assets liefern still unvollständige
  Preise. Chunking einbauen.
- Kein `client.Timeout`, keine Polly-Policies. → Typed Clients via
  `IHttpClientFactory` + `AddStandardResilienceHandler()`
  (Microsoft.Extensions.Http.Resilience).
- JSON-Parsing durchweg mit manuellem `JsonElement.GetProperty` — wirft
  `KeyNotFoundException` bei API-Änderungen. DTOs mit `JsonPropertyName`
  (wie bei den Bitpanda-Modellen schon vorhanden) konsequent nutzen.
- `decimal.TryParse(balance, …)` in `GetRippleAvailableBalances:214` ohne
  `CultureInfo.InvariantCulture` — auf Systemen mit `,`-Dezimaltrenner falsch.

## Q4 — Naming, toter Code, Kleinkram 🟡

| Fundstelle | Punkt |
|---|---|
| `AssetMeasuringDto.cs:78` | `IntegrationShit` → `IntegrationAmount`; ist Teil des generierten TS-Clients |
| `AssetMeasuringDto.cs:26` | `MessungDto` → `AssetHoldingDto` (API-weit deutsch/englisch gemischt) |
| `HttpClientExtensionMethods.cs:1` | Namespace `ImmichFrame.Core.Helpers` aus fremdem Projekt |
| `CryptoTrackerLogic.cs:52–53` | Auskommentierter Code (Bitpanda-Portfolio) |
| `CryptoTrackerLogic.cs:142–201` | Cardano: `throw NotImplementedException` mit unerreichbarem Code dahinter |
| `AlphaVantageStockLogic.cs` | Toter Code (nie registriert); `TwelveDataSharp`-Package ungenutzt |
| `cryptotracker.worker/` | Legacy-Duplikat der Import-Logik → löschen |
| `AssetController.cs:119` | `}; ;` Doppel-Semikolon; `AssetController.cs:165` `= null; ;` |
| `AssetController.cs:15` | `ILogger<CryptoTrackerController>` in fremden Controllern (Copy-Paste; auch Integration/Measuring) |
| `MeasuringController.cs:19` | `_cryptoTrackerLogic` injiziert und nie benutzt (auch IntegrationController) |
| `Asset.cs:17` | `AssetType`-Enum: `[Description]`-Attribute werden nirgends gelesen |
| `Program.cs:225` | Kommentar „apply apply migrations" |
| Logging durchweg | String-Interpolation (`$"…"`) statt strukturierter Templates — AuthController macht es richtig vor |
| `FiatLogic.cs:48` | Vergleich `fiatSymbols == baseCurrency.ToLower()` funktioniert nur, wenn genau eine Währung angefragt wurde — fragile Kurzschluss-Logik |
| `CryptoTrackerConfig` | `Interval` ohne Validierung: `interval: 0` → `TimeSpan.Zero` → `PeriodicTimer`-Exception beim Start |

## Q5 — `UpdateService`-Verhalten hinterfragt 🟡

- **Erster Import erst nach einem vollen Intervall?** Nein — die Schleife
  importiert sofort und wartet danach (`ExecuteAsync`-Reihenfolge). Aber: der
  Import blockiert den Anwendungsstart nicht, gut. Was fehlt: **Jitter/Alignment**.
  Mit `interval: 120` hängt es von der Startzeit ab, ob der letzte Lauf des Tages
  um 22:xx oder 23:5x UTC passiert — der „Tagesendstand" ist zufällig. Für einen
  Tages-Snapshot wäre „einmal täglich zu fester Uhrzeit, plus on-demand" die
  ehrlichere Semantik (siehe [03/D2](03-datenmodell-und-aggregation.md)).
- **Kein manueller Trigger:** Nach dem Anlegen einer Integration/eines Assets
  muss man bis zu `interval` Minuten warten. Ein `POST /api/admin/sync`-Endpoint
  (mit Lock gegen Parallellauf) wäre billig und nimmt viel UX-Druck.
- Fehler eines Laufs brechen die Schleife nicht (gut), aber es gibt keinerlei
  Sichtbarkeit: kein „letzter erfolgreicher Sync"-Status, den die UI anzeigen
  könnte. `SyncRun`-Tabelle (Start, Ende, Status, Fehler je Integration) erwägen.

## Q6 — OpenAPI/Client-Generierung 🟡

- `openApi/swagger.json` ist eingecheckt und wird manuell per `make api` (laufende
  API vorausgesetzt) aktualisiert — Drift zwischen Backend und generiertem Client
  fällt erst zur Laufzeit auf. Besser: Swagger-JSON im Build erzeugen
  (`dotnet swagger tofile` oder `Microsoft.Extensions.ApiDescription.Server`)
  und den TS-Client in CI auf Aktualität prüfen.
- Controller-Rückgaben wie `Task<List<Asset>>` (EF-Entity) und structs mit
  öffentlichen Settern erzeugen schwache OpenAPI-Schemata (alles nullable/optional).
  Mit DTOs + `[ProducesResponseType]` wird der generierte Client deutlich besser.
