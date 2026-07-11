# Testing & DevOps

## T1 — Testlage 🔴 → weitgehend behoben

> **Status 2026-07-10:** Die Prioritätenliste ist abgearbeitet — 71 Tests grün:
> `PortfolioQueryServiceTest` (Fill-Grenze, 0-Filter, Batch),
> `FrankfurterCurrencyPriceProviderTest` (Kursrichtung/Bug-1-Regression, Cache),
> `CoingeckoPriceProviderTest` (Mapping, Cache, Fehlerpfade),
> `UpdateServiceTest` (Fehler-Isolation, Idempotenz, AssetType-Erkennung,
> Provider-Dispatch), `AssetServiceTest`, `IntegrationServiceTest`,
> `MeasuringServiceTest`, `YamlConfigurationTest`.

**Noch offen:**

- **Auth** ist ungetestet (Login/Lockout/First-User-Setup/JWT).
- ~~Tagesgrenzen-Szenarien~~ ✅ 2026-07-11: Tests laufen auf gepinnter Fake-Zeit
  (`TestClock`/`PortfolioClock`, [Bug 6](01-kritische-bugs.md))
  inkl. Mitternachts-Grenzfällen; keine Abhängigkeit von der Systemzeit mehr.

## T2 — InMemory-Provider verdeckt genau die vorhandenen Bugs 🟠

`UseInMemoryDatabase` (alle webapi-Tests) unterscheidet sich von Postgres genau
dort, wo dieses Projekt seine Probleme hat:

- **Case-Sensitivity von Strings** („chf" vs „CHF", Bug 3) — InMemory vergleicht
  wie .NET, Postgres wie Postgres.
- **Transaktionen werden ignoriert** (das Warning wird im Test explizit
  unterdrückt, Zeile 28) — Rollback-Verhalten des Imports ist untestbar.
- **DateTime-Kind-Anforderungen von Npgsql** (Bug 6) tauchen nie auf.

**Empfehlung:** Für DB-nahe Tests **Testcontainers-for-.NET** mit echtem
Postgres (ein Container pro Testlauf, ~2 s Overhead). InMemory nur für reine
Service-Logik ohne Query-Semantik behalten — oder ganz ersetzen. Die
Zwei-Zeilen-Migrationshistorie macht das Setup einfach.

## T3 — Keine CI 🟠

Es gibt kein `.github/workflows/` — kein Build-Check, keine Tests, kein Lint
auf PRs (die Repo-Historie zeigt PR-basierte Arbeit, d. h. CI hätte Nutzen).

**Empfehlung — minimale Pipeline (1 Datei):**

```yaml
# .github/workflows/ci.yml (Skizze)
on: [push, pull_request]
jobs:
  backend:
    - dotnet build --warnaserror   # TreatWarningsAsErrors schrittweise aktivieren
    - dotnet test (beide Testprojekte)
  frontend:
    - npm ci && npm run lint && npm run check && npm run build
  docker (nur main/tags):
    - buildx build --target final   # Push auf Docker Hub via Secret
```

Dazu: `dotnet format`-Check oder `.editorconfig` mit Analyzern
(`Microsoft.CodeAnalysis.NetAnalyzers` ist implizit da — `AnalysisLevel` in
`Directory.Build.props` hochsetzen), Dependabot/Renovate für die vielen
Exchange-SDK-Pakete.

## T4 — Deployment/Betrieb 🟡

- **Kein Healthcheck-Endpoint.** `MapHealthChecks("/healthz")` + DB-Check
  (`AddDbContextCheck`) — wichtig für Docker `HEALTHCHECK`/Orchestrierung.
- `db.Database.Migrate()` beim Start: blockiert den Start, racet bei >1 Replica,
  und ein Migrationsfehler nimmt die API komplett runter. Für Self-Hosting ok,
  aber mindestens hinter ein Config-Flag (`database.autoMigrate: true`).
- **Kein Graceful-Degradation-Signal:** Wenn CoinGecko/Frankfurter down sind,
  sieht der Nutzer stumm veraltete Werte (Forward-Fill). Ein Status-Endpoint
  („letzter erfolgreicher Sync pro Integration", siehe Q5) + UI-Banner.
- Dockerfile ist ordentlich (Multi-Stage, Non-Root, Multi-Arch via TARGETARCH).
  Kleinigkeiten: `dotnet restore` ohne `--locked-mode`/ohne Layer-Trennung
  (Copy nur der csproj vor restore würde den Cache verbessern); `npm ci` läuft
  vor dem Copy des Quellcodes — gut gelöst.
- **Backups:** nirgends erwähnt. Für ein Finanz-Tracking-Tool gehört ein
  `pg_dump`-Hinweis (oder Compose-Beispiel mit Backup-Sidecar) ins README.
- Versionierung: `janmer/cryptotracker_web:latest`/`:dev` ohne semantische Tags —
  Rollback unmöglich. Git-Tag → Image-Tag in der CI koppeln.

## T5 — Repo-Hygiene 🟡

- `CLAUDE.md` beschreibt `make test-webapi`/`test-core` — beide laufen, aber
  `make api` setzt eine laufende API voraus (nicht CI-fähig, siehe
  [05/Q6](05-backend-codequalitaet.md)).
- ~~`cryptotracker.worker` aus der Solution und dem Repo entfernen~~ ✅ erledigt 2026-07-09 (Details
  [04/A4](04-architektur.md)).
- `Directory.Build.props` prüfen: `Nullable`/`ImplicitUsings` scheinen aktiv,
  aber es existieren unterdrückte Warnungen (non-nullable Navigation
  `AssetMeasuring.Integration`). Ziel: Build ohne Warnungen, dann
  `TreatWarningsAsErrors`.
- README: Coinbase/Bitpanda/… Key-Berechtigungen („read-only") und der
  Privacy-Hinweis zu XPUB-Abfragen ([02/S6](02-sicherheit.md)) fehlen.
