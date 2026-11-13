# Backlog

Keine Spec und keine Aufgabenliste, solange niemand danach fragt. Erledigte Pläne stehen in der Git-Historie.

## Entscheidungen

Nicht umsetzen, ohne dass es ausdrücklich gewollt ist.

- **Single-Tenant ist der Ist-Zustand.** Registrierung ist zu, sobald ein User existiert (`auth.allowregistration` öffnet sie wieder). `Asset`, `ExchangeIntegration` und `DailyHolding` haben keinen User-Bezug. Multi-User hiesse `UserId` auf Integrationen, Filter in jeder Query, Migration der Bestände.
- **`Asset.Symbol` ist Primärschlüssel.** Symbole kollidieren über Anlageklassen („EUR", Aktienticker). Ein Rename kaskadiert. Alternative: Surrogate `Guid`, Unique `(Symbol, AssetType)`, eine Normalisierung beim Import.
- **`AssetPriceHistory.Currency` suggeriert mehrere Währungen.** Abfragen filtern immer auf `basecurrency`. Alternative: Spalte streichen, eine Basiswährung pro Installation. Ein Wechsel invalidiert die Historie, es gibt keine Umrechnung.
- **API-Integrationen leben in `config.yml` und matchen per Name.** Die UI kann die DB-Zeile löschen, der nächste Sync legt sie wieder an. Umbenennen ist gesperrt. Alternative: Integrationen in die DB, Credentials verschlüsselt (Data Protection), Config nur als Bootstrap.
- **`Commodity` und `RealEstate` haben keinen Preis-Provider.** Manuellen Preis bauen oder die Werte streichen.

## Code

- Fehler werden als `KeyNotFoundException` und `InvalidOperationException` geworfen und enden als 500. ProblemDetails und 4xx fehlen. `GetIntegrationDetails` liefert bei unbekannter Id `null` (200, leerer Body).
- Kein `CancellationToken` in Controllern oder im Import. `AddAsync` für normale Inserts. EF-Log über `LogTo(Console.WriteLine)`.
- CoinGecko ohne Retry und ohne Chunking (Default 100 Ids, darüber fehlen Preise still). Kein `HttpClient.Timeout`. `RippleIntegrationProvider` parst `decimal` ohne `InvariantCulture`.
- Preiszeilen werden unfenstert geladen. Für `[from, to]` reichen die Zeilen im Fenster plus die letzte davor. Index `(Symbol, Currency, Date DESC)`.
- Fehlender Preis wird als `0m` gerechnet (`PortfolioQueryService`). „Unbekannt" und 0 sind nicht unterscheidbar.
- `report/+page.ts` schickt `toISOString()` an `GetMeasuringsByDate`. Der Endpoint sollte `yyyy-MM-dd` nehmen.
- `ExchangeIntegration.IsHidden` wird nie gefiltert. `decimal(18,10)` gilt global und ist für grosse Summen und Micro-Caps knapp.
- Import-Takt hängt an der Startzeit. Kein manueller Sync-Endpoint. `LastSyncedAtUtc` sagt nicht, ob der Lauf fehlschlug.
- `make api` braucht eine laufende Development-API. Structs mit öffentlichen Settern erzeugen schwache OpenAPI-Schemata.
- Exchange-SDK-Clients werden pro Abruf von Hand erzeugt, nicht über `AddCoinbase()` und Verwandte.
- `interval: 0` wirft im `PeriodicTimer`. Auskommentierter Bitpanda-Portfolio-Call. `[Description]` auf `AssetType` wird nicht gelesen. Frankfurter-Kurzschluss gilt nur bei genau einer Währung.
- Deutschreste und native `<select>`s in `assets/[slug]` und `assets/[slug]/edit`. `auth/register` nutzt alte Tailwind-Klassen, nicht die Form-Primitives.
- Loads noch in `onMount`: `integrations/[slug]`, `integrations/[slug]/measurings`, `assets/[slug]`.
- JWT: kein Refresh (Default 60 Minuten). Ohne konfigurierten Issuer wird der Request-Host zum Issuer und nicht validiert. `Me()` sucht über die E-Mail-Claim, nicht über eine User-Id.
- `KnownProxies` und `KnownIPNetworks` sind geleert. Jeder Client kann `X-Forwarded-Host`/`-Proto` setzen. Das fliesst in den Issuer und in das `Secure`-Flag des Cookies.

## Betrieb

- Keine CI. Minimum: `dotnet test` beider Testprojekte, im Frontend `npm run lint`, `npm run check`, `npm run build`.
- Auth ist ungetestet (Login, Lockout, First-User-Setup, JWT). Webapi-Tests nutzen EF InMemory und sehen keine Postgres-Semantik (Vergleich, Transaktionen, `DateTimeKind`). Für DB-nahe Tests Testcontainers.
- Kein Frontend-Test (Vitest/Playwright).
- Kein Healthcheck. `Database.Migrate()` und `UpdateService` laufen bei jedem Start, unkoordiniert. Mehr als eine Instanz bräuchte ein Lock und ein Migrate-Flag.
- Image-Tags nur `:latest` und `:dev`. Kein Backup-Hinweis (`pg_dump`).
- README fehlt: Exchange-Keys read-only, und dass Bitcoin/Ethereum/Ripple Adressen bzw. XPUB/ZPUB an blockchain.info, ethplorer.io und xrpscan.com schicken.
