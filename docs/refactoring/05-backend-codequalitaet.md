# Backend — Code-Qualität

Ergänzend zu den strukturellen Punkten in [04-architektur.md](04-architektur.md);
hier die handwerklichen Themen, jeweils mit Fundstelle.

<a id="fehlerbehandlung"></a>
## Q1 — Fehlerbehandlung: Exceptions ohne Status-Mapping 🟠

> **Teilstatus 2026-07-10:** Die Services werfen inzwischen typisiert und
> englisch (`KeyNotFoundException` für „nicht gefunden",
> `InvalidOperationException` für fachliche Konflikte) — aber ohne Middleware
> enden weiterhin alle als 500er.

**Empfehlung (Rest):**

- Exception-Middleware + `ProblemDetails` (`AddProblemDetails()` ist in .NET 10
  Standardkost); `KeyNotFoundException` → 404, `InvalidOperationException` → 400/409.
- Rückgabetypen ehrlich machen: `Task<bool>`, das nie `false` zurückgibt
  (`AddAsset`, `DeleteAsset`, `SetVisibilityForSymbol`, …) → `ActionResult` mit
  201/204/404. `GetIntegrationDetails` gibt bei unbekannter Id `null` → 200 mit
  leerem Body statt 404.

## Q2 — Async/EF-Hygiene 🟠

- Keine einzige Query nutzt `AsNoTracking()`; die Aggregation trackt tausende
  Entities (siehe [03/D3](03-datenmodell-und-aggregation.md)).
- **Kein `CancellationToken` im gesamten Backend** — weder in Controllern noch
  in `UpdateService.Import` (der Token wird bei `ExecuteAsync` entgegengenommen
  und dann ignoriert bis zum `WaitForNextTickAsync`). Bei langsamen Exchange-APIs
  blockiert der Shutdown.
- `db.AssetMeasurings.AddAsync(...)`: `AddAsync` ist nur für ValueGenerators
  nötig; `Add` reicht — Mikropunkt, aber überall.
- `Program.cs`: `LogTo(Console.WriteLine)` umgeht das Logging-Framework
  (doppelte Ausgabe zu `AddFilter("Microsoft.EntityFrameworkCore", Warning)`).

## Q3 — HTTP-Aufrufe: keine Timeouts, kein Retry, kein Rate-Limit-Umgang 🟠

- CoinGecko free tier: ~5–15 req/min. Listing (unpaginiert, ~2 MB JSON, seit
  Bug 9 immerhin 24h gecacht) und Quotes werden ohne Backoff aufgerufen; bei
  429 **wirft** der Provider inzwischen (statt still leer) → der Metadaten-Lauf
  der Runde fällt aus. Retry/Backoff fehlt weiterhin.
- `CoingeckoPriceProvider.GetQuotesAsync` baut **alle** externen Ids in eine URL
  — bei vielen Assets drohen URL-Längen-Limits und CoinGecko paginiert die
  Antwort (per_page Default 100) — mehr als 100 Assets liefern still unvollständige
  Preise. Chunking einbauen.
- Kein `client.Timeout`, keine Polly-Policies. → Typed Clients via
  `IHttpClientFactory` + `AddStandardResilienceHandler()`
  (Microsoft.Extensions.Http.Resilience).
- JSON-Parsing durchweg mit manuellem `JsonElement.GetProperty` — wirft
  `KeyNotFoundException` bei API-Änderungen. DTOs mit `JsonPropertyName`
  (wie bei den Bitpanda-Modellen schon vorhanden) konsequent nutzen.
- `decimal.TryParse(balance, …)` im `RippleIntegrationProvider` ohne
  `CultureInfo.InvariantCulture` — auf Systemen mit `,`-Dezimaltrenner falsch.

## Q4 — Naming, toter Code, Kleinkram 🟡

Die meisten Punkte dieser Liste sind mit A1–A6 erledigt (Renames, tote
Codepfade, Logger-Kategorien, ungenutzte Injektionen, Message-Templates in den
Services — siehe [04](04-architektur.md)). Übrig:

| Fundstelle | Punkt |
|---|---|
| `BitpandaIntegrationProvider.cs` | Auskommentierter Code (Bitpanda-Portfolio-Endpoint) — nutzen oder löschen |
| `Asset.cs:17` | `AssetType`-Enum: `[Description]`-Attribute werden nirgends gelesen |
| `Program.cs` | Kommentar „apply apply migrations" |
| Preis-/Integration-Provider | Logging teils noch String-Interpolation (`$"…"`) statt strukturierter Templates (Frankfurter, Yahoo) |
| `FrankfurterCurrencyPriceProvider.cs:83` | Vergleich `externalIdsQuery == baseCurrency.ToLower()` funktioniert nur, wenn genau eine Währung angefragt wurde — fragile Kurzschluss-Logik |
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
- ~~EF-Entities als Controller-Rückgaben~~ ✅ erledigt 2026-07-10 (`AssetDto`).
  Offen: structs mit öffentlichen Settern erzeugen weiter schwache
  OpenAPI-Schemata (alles nullable/optional) — `[ProducesResponseType]` und
  `required`-Properties würden den generierten Client verbessern.
