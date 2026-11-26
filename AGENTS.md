# AGENTS.md

Selbst gehosteter Portfolio-Tracker. Befehle stehen im `Makefile`.

## Nicht raten

- `cryptotracker.core` hängt von `database` ab, nicht umgekehrt. EF-Entities liegen in `database`. Import und Abfragen (`UpdateService`, `PortfolioQueryService`, `IntegrationService`) bleiben in `webapi`. Auth ist JWT, optional OIDC; MCP hängt unter `/mcp`.
- Kein `svelte.config.js`. adapter-static steht in `cryptotracker.web/vite.config.ts` (Output `build/`, Fallback `index.html`). `src/routes/+layout.ts` setzt `ssr = false` und `prerender = false`.
- `docs/backlog.md` ist offenes Backlog, keine Spec und keine Aufgabenliste. Das Snapshot-Modell heisst `DailyHolding`, nicht `AssetMeasuring`.

## Start

- API: `make dev` (http://localhost:5106, .NET 10, PostgreSQL).
- Ohne `CONFIG_PATH` sucht Development `../config` relativ zum Prozess-Arbeitsverzeichnis, Production `./config`. `dotnet run --project` wechselt das Verzeichnis nicht: `make dev` aus dem Repo-Root findet `config/config.yml` nicht. Aus `cryptotracker.webapi` starten oder `CONFIG_PATH` auf das Repo-`config/` setzen. Eine fehlende Datei wird still übersprungen; der Fehler kommt erst als fehlender Connection-String.
- `config/config.yml` und `config.json` sind gitignored. Vorlage: `config/example-config.yml`. Secrets nur dort oder über `CRYPTOTRACKER_`-Env (`__` für Verschachtelung, überschreibt die Datei).
- Startup wirft bei fehlendem oder zu kurzem `auth.secret` (< 32 Bytes), ungültiger Zeitzone, leeren oder doppelten Integrationsnamen, Source ohne gültigen `type`, und bei MCP ohne Token ≥ 32 Zeichen.
- `Database.Migrate()` und `UpdateService` laufen bei jedem Start und sind nicht über Replicas koordiniert. Nur eine Instanz.
- Frontend: in `cryptotracker.web` `npm run dev` (Node 24, http://localhost:5173, proxied nur `/api`). SpaProxy in der csproj zeigt auf `https://localhost:5173`, Vite spricht HTTP — API und Vite getrennt starten.
- `make docker_build_web` (`:latest`) und `make docker_build_web_dev` (`:dev`) bauen dasselbe Dockerfile-Target `final`; nur der Tag unterscheidet sich. Der Frontend-Build landet in `wwwroot`, der Container lauscht auf 8080.
- Tests ohne Postgres: `make test-webapi`, `make test-core` (NUnit, Moq; Webapi-Tests mit EF InMemory, Transaktionen werden dort ignoriert). Ein Test: `dotnet test cryptotracker.webapi.tests --filter FullyQualifiedName~UpdateServiceTest`.

## Verträge

- Nach API-Änderungen, solange die API in Development läuft (Swagger nur dort): `make api`. oazapfts schreibt `openApi/swagger.json` und `cryptotracker.web/src/lib/cryptotrackerApi.ts`. Beide Dateien sind eingecheckt; die TS-Datei nicht von Hand ändern. JSON: camelCase, Enums als Strings.
- Migrationen: `make ef_add_migration Name`, `make ef_remove_migration`, `make ef_update_database [Name]`. Ohne Make immer `--project cryptotracker.database --startup-project cryptotracker.webapi`. EF-Tools führen `Program.cs` bis `app.Run()` aus und brauchen deshalb gültige Config und Postgres. Migrationsdateien nicht von Hand editieren.
- Paketversionen nur in `Directory.Packages.props` (`PackageReference` ohne `Version`). TFM `net10.0` kommt aus `Directory.Build.props`.

## Bestände

- Den Portfolio-Tag nur über `PortfolioClock` bestimmen (Config-Zeitzone, Default `Europe/Zurich`), nicht über `DateTime.Now` oder `UtcNow.Date`. Tests: `TestClock` / `FixedTimeProvider`. `DateTimeKind.Unspecified` gilt als UTC, weil Npgsql sonst `timestamptz` ablehnt.
- PK von `DailyHolding` ist `(IntegrationId, Symbol, Date)`. Imports upserten. Positionen, die eine Börse nicht mehr meldet, werden mit Amount 0 geschrieben.
- Sync-Bestände höchstens `maxfilldays` (Default 10) vortragen. Manuelle Integrationen (`IsManual`, nur in der DB) bleiben bis zum nächsten Eintrag gültig. Config-Integrationen matchen case-insensitive über `name` und dürfen über die API nicht umbenannt werden. Mehrere `sources` einer Integration werden pro Symbol addiert.
- `AssetMetadata.Price` ist der Wert einer Einheit in `Currency` — für Fiat, Crypto und Aktien gleich. Frankfurter und CoinGecko sind immer registriert; Aktien nur bei `stockapi: yahoofinance`.
- Registrierte Provider: Bitpanda, Coinbase, Binance, KuCoin (`passphrase`), Crypto.com, Bitcoin (xpub/zpub), Ethereum, Base, Arbitrum, Optimism, Polygon, Gnosis, Ripple. `Cardano` ist nur ein Enum-Wert. Börsen ausser Bitpanda laufen über CryptoClients.Net; Bitpanda, Bitcoin, Ripple und die EVM-Chains über `HttpClient`. EVM-Bestände kommen von Blockscout, nur die native Münze.

## UI

- `npm run check`, `npm run lint` (Prettier und ESLint), `npm run format`. Prettier: Tabs, doppelte Quotes, keine trailing commas, printWidth 100.
- Imports über `#lib/...js` (`package.json` `imports`), nicht `$lib`.
- In `load` die generierte API synchron innerhalb von `runInLoad(fetch, () => …)` aufrufen. Ein `await` davor setzt SvelteKits `fetch` schon zurück. Mutationen über `mutate()` (`#lib/api/mutate.js`).
- shadcn-svelte, Style `luma`, unter `src/lib/components/ui`. Charts über LayerChart, nicht Chart.js. Tailwind 4 läuft als Vite-Plugin; `@import "tailwindcss"` nicht an einen anderen CSS-Importer hängen.
