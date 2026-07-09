# Testing & DevOps

## T1 — Testlage: der komplexeste Code ist ungetestet 🔴

**Ist:**

- `cryptotracker.core.tests`: eine Datei mit `Assert.Pass()` — Platzhalter,
  testet nichts (`AssetTest.cs`).
- `cryptotracker.webapi.tests`: solide CRUD-Tests für den `AssetController`
  (InMemory-DB, Moq) plus ein kleiner Logic-Test. Gut als Muster, aber:
- **Null Tests für die Aggregation** (`ApiHelper`) — das ist mit Abstand der
  fehleranfälligste Code (Forward-Fill, Tagesgrenzen, Batch-Fenster; siehe
  Bugs 1–3, 6). Genau die Bugs aus [01](01-kritische-bugs.md) hätten Tests
  gefunden.
- Null Tests für `UpdateService.Import` (Löschen/Schreiben, Fehlerpfade),
  `CryptoTrackerAssetLogic` (Metadaten-Matching), `FiatLogic` (Kursrichtung!),
  Auth.

**Empfehlung — Prioritätenliste neuer Tests:**

1. `PortfolioQueryService` (heute `ApiHelper`): Tabellen-getriebene Tests über
   ein fixes Szenario (mehrere Integrationen, Lücken, verkaufte Assets,
   Tagesgrenzen um Mitternacht UTC/lokal). Voraussetzung: `TimeProvider`
   injizieren statt `DateTime.Now`.
2. `FiatLogic`/Preis-Provider: Semantik „Preis = Wert einer Einheit in
   Basiswährung" als Kontrakt-Test gegen gemockte HTTP-Responses
   (`HttpMessageHandler`-Fake) — verhindert Regression von Bug 1.
3. Import-Pipeline: Provider-Fake liefert Fehler/leer/Teilmengen → erwartete
   Snapshot-Zustände (fixiert Bug 5-Verhalten).

## T2 — InMemory-Provider verdeckt genau die vorhandenen Bugs 🟠

`UseInMemoryDatabase` (WebApiTest.cs:27) unterscheidet sich von Postgres genau
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
- `cryptotracker.worker` aus der Solution und dem Repo entfernen (Details
  [04/A4](04-architektur.md)).
- `Directory.Build.props` prüfen: `Nullable`/`ImplicitUsings` scheinen aktiv,
  aber es existieren unterdrückte Warnungen (non-nullable Navigation
  `AssetMeasuring.Integration`). Ziel: Build ohne Warnungen, dann
  `TreatWarningsAsErrors`.
- README: Coinbase/Bitpanda/… Key-Berechtigungen („read-only") und der
  Privacy-Hinweis zu XPUB-Abfragen ([02/S6](02-sicherheit.md)) fehlen.
