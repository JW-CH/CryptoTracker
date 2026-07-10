# Kritische Bugs

Diese Punkte produzierten **falsche Zahlen oder Datenverlust**. Stand 2026-07-10
ist davon nur noch **Bug 6** offen — die erledigten Punkte stehen unten als
Kurzprotokoll (Details und ursprüngliche Befunde in der Git-Historie dieses
Dokuments).

---

<a id="bug-6"></a>
## Bug 6 — UTC/Lokalzeit-Mischung verschiebt Tagesgrenzen 🟠 (offen)

**Betroffen (Auswahl, Stand nach den Service-Umbauten):**

| Stelle | Zeitbasis |
|---|---|
| `UpdateService.Import` (Tages-Löschung, Messung) | `DateTime.UtcNow` |
| `CryptoTrackerController` („heute") | `DateTime.Now` (Serverzeit) |
| `AssetMetadataService.ApplyMetadataAsync` (Preis-Datum) | `DateTime.Now` |
| `IntegrationService.GetIntegrationDetailsAsync` („heute") | `DateTime.Now` |
| `MeasuringService.AddIntegrationMeasuringAsync` (manuelle Messung) | `dto.Date.Date` (Client-Kind) |
| `PortfolioQueryService` (Tagesgrenzen) | `DateOnly` → UTC-Mitternacht |

Im Docker-Container läuft der Server auf UTC, lokal auf Europe/Zurich — dieselbe
Abfrage liefert je nach Umgebung andere „Tage". Eine Messung um 23:30 UTC gehört
lokal schon zum Folgetag; die Lösch-Logik des Imports (UTC-Tag) und die Anzeige-
Logik (lokaler Tag) schneiden unterschiedlich.

Zusätzlich ein Laufzeitrisiko: `MeasuringService.AddIntegrationMeasuringAsync`
schreibt `dto.Date` direkt in eine `timestamptz`-Spalte. Npgsql wirft für
`DateTimeKind.Local`/`Unspecified` eine `InvalidCastException` — ob der Client ein
`Z`-Suffix schickt, entscheidet also über Erfolg oder 500er. **Verifizieren und
normalisieren** (`DateTime.SpecifyKind(..., Utc)` bzw. `ToUniversalTime()`).

**Fix-Prinzip:** Speicherung konsequent UTC; „Portfolio-Tag" ist eine fachliche
Entscheidung (empfohlen: konfigurierbare Anzeige-Zeitzone, Default Europe/Zurich)
und wird an genau einer Stelle aus UTC abgeleitet. `TimeProvider` injizieren statt
statischer `DateTime.Now`-Aufrufe — macht das auch endlich testbar (die
`AsyncCache`/`FakeTimeProvider`-Infrastruktur aus dem Bug-9-Fix ist ein Anfang).

---

## Erledigt (Kurzprotokoll)

| Bug | Fix | Anmerkung |
|---|---|---|
| **1** — Fiat-Bewertung invertiert 🔴 | Kurs als `1/rate` gespeichert, Kontrakt „`AssetMetadata.Price` = Wert von 1 Einheit in Basiswährung" dokumentiert + getestet | behoben 2026-07-09. **Altdaten bewusst nicht migriert** (Single-User-Entscheidung): Fiat-Zeilen vor dem Fix-Datum bleiben invertiert |
| **2** — Verkaufte Assets zählen ewig 🔴 | Import schreibt 0-Messungen für verschwundene Assets; Forward-Fill auf `maxfilldays` (Default 10) begrenzt | behoben 2026-07-09 |
| **3** — Währungs-Casing chf/CHF 🟠 | `basecurrency` konfigurierbar, Setter normalisiert lowercase, Preis-Lookups filtern auf Currency | behoben 2026-07-09. Alte `CHF`-Zeilen bewusst nicht migriert (wie Bug 1) |
| **4** — Auto-Assets immer Crypto 🟠 | `BalanceResult.AssetType?`-Hinweis von der Quelle (Bitpanda, Blockchains) + Frankfurter-Fallback für Misch-Exchanges | behoben 2026-07-10 |
| **5** — Fehlabruf löscht Tagesdaten 🟠 | Provider werfen statt leere Listen; fetch-before-delete; Transaktion + Fehler-Isolation pro Integration | behoben 2026-07-09 |
| **7** — Frankfurter-Host umgezogen 🟡 | Default-BaseUrl `api.frankfurter.dev/v1` | behoben 2026-07-09 |
| **8** — `GetAsset` ohne Currency-Filter 🟡 | Preis-Lookups filtern auf `BaseCurrency` | behoben 2026-07-09 (mit Bug 3) |
| **9** — Caches ohne TTL in Singletons 🟡 | `IMemoryCache` (24h TTL) in beiden Listen-Providern; Fehler werfen und werden nie gecacht | behoben 2026-07-10. Rest-Risiko Doppel-Fetch bei parallelem Erst-Aufruf bewusst akzeptiert |
| **10** — Dashboard-Seiteneffekt 🟡 | Symbolliste wird pur aus den Response-Daten berechnet statt Set-Mutation im `{#each}`-Template | behoben 2026-07-10 |
