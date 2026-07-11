# Kritische Bugs

Diese Punkte produzierten **falsche Zahlen oder Datenverlust**. Stand 2026-07-11
sind **alle behoben** — Details und ursprüngliche Befunde stehen in der
Git-Historie dieses Dokuments.

## Erledigt (Kurzprotokoll)

| Bug | Fix | Anmerkung |
|---|---|---|
| **1** — Fiat-Bewertung invertiert 🔴 | Kurs als `1/rate` gespeichert, Kontrakt „`AssetMetadata.Price` = Wert von 1 Einheit in Basiswährung" dokumentiert + getestet | behoben 2026-07-09. **Altdaten bewusst nicht migriert** (Single-User-Entscheidung): Fiat-Zeilen vor dem Fix-Datum bleiben invertiert |
| **2** — Verkaufte Assets zählen ewig 🔴 | Import schreibt 0-Messungen für verschwundene Assets; Forward-Fill auf `maxfilldays` (Default 10) begrenzt | behoben 2026-07-09 |
| **3** — Währungs-Casing chf/CHF 🟠 | `basecurrency` konfigurierbar, Setter normalisiert lowercase, Preis-Lookups filtern auf Currency | behoben 2026-07-09. Alte `CHF`-Zeilen bewusst nicht migriert (wie Bug 1) |
| **4** — Auto-Assets immer Crypto 🟠 | `BalanceResult.AssetType?`-Hinweis von der Quelle (Bitpanda, Blockchains) + Frankfurter-Fallback für Misch-Exchanges | behoben 2026-07-10 |
| **5** — Fehlabruf löscht Tagesdaten 🟠 | Provider werfen statt leere Listen; fetch-before-delete; Transaktion + Fehler-Isolation pro Integration | behoben 2026-07-09; seit dem Snapshot-Upsert (Bug 6/D2) wird strukturell nie gelöscht |
| **6** — UTC/Lokalzeit-Mischung verschiebt Tagesgrenzen 🟠 | `PortfolioClock` (TimeProvider + konfigurierbare `timezone`, Default Europe/Zurich) als einzige Tages-Ableitung, kein `DateTime.Now` mehr; dann [D2-Snapshot-Modell](03-datenmodell-und-aggregation.md): `DailyHolding` mit PK `(IntegrationId, Symbol, Date)`, Tag wird beim Schreiben materialisiert. Tests auf gepinnter Fake-Zeit | behoben 2026-07-11. **Altdaten bewusst verworfen** (Single-User-Entscheidung): Messungen + Preishistorie per Migration geleert, Neuaufbau ab erstem Import |
| **7** — Frankfurter-Host umgezogen 🟡 | Default-BaseUrl `api.frankfurter.dev/v1` | behoben 2026-07-09 |
| **8** — `GetAsset` ohne Currency-Filter 🟡 | Preis-Lookups filtern auf `BaseCurrency` | behoben 2026-07-09 (mit Bug 3) |
| **9** — Caches ohne TTL in Singletons 🟡 | `IMemoryCache` (24h TTL) in beiden Listen-Providern; Fehler werfen und werden nie gecacht | behoben 2026-07-10. Rest-Risiko Doppel-Fetch bei parallelem Erst-Aufruf bewusst akzeptiert |
| **10** — Dashboard-Seiteneffekt 🟡 | Symbolliste wird pur aus den Response-Daten berechnet statt Set-Mutation im `{#each}`-Template | behoben 2026-07-10 |
