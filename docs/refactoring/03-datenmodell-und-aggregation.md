# Datenmodell & Aggregation

Das ist das Kernstück des Refactorings. Die Grundidee des Modells ist richtig
(Bestände und Preise getrennt, Wert zur Abfragezeit berechnet) — aber mehrere
Design-Entscheidungen verdienen es, hinterfragt zu werden.

## Ist-Zustand (nach dem D2-Umbau 2026-07-11)

```
Asset                    DailyHolding                       AssetPriceHistory
─────                    ────────────                       ─────────────────
Symbol (PK)   ◄──────┐   (IntegrationId, Symbol, Date) PK   (Symbol, Date, Currency) PK
ExternalId           ├── Symbol (FK)                        Symbol (FK) ─────► Asset
Name                 │   IntegrationId (FK) ──► ExchangeIntegration
Image                │   Date (DateOnly, Portfolio-Tag)     Date (DateOnly)
AssetType            │   Amount                             Currency
IsHidden             │   Source (Sync | Manual)             Price
                     │   RecordedAtUtc (Audit)

ExchangeIntegration: Id (Guid PK), Name, Description, IsManual, IsHidden
```

Datenfluss:

1. `UpdateService` läuft alle `interval` Minuten, holt pro Config-Integration
   die Balances (via `IIntegrationProvider`) und **upsertet** pro
   `(Integration, Symbol, Portfolio-Tag)` genau einen Snapshot (inkl. 0-Zeilen
   für verschwundene Assets, siehe Bug 2). Der Portfolio-Tag kommt aus der
   `PortfolioClock` (konfigurierbare Zeitzone, Bug 6).
2. `AssetMetadataService` schreibt/aktualisiert pro Asset genau eine
   Preiszeile pro Tag (Basiswährung aus Config, lowercase-normalisiert).
3. Lesend nimmt `PortfolioQueryService.GetAssetDayMeasuringBatchAsync` pro
   `(Symbol, Integration)` den letzten Snapshot ≤ Stichtag, multipliziert mit
   dem letzten Preis ≤ Stichtag (Forward-Fill, begrenzt auf `maxfilldays`) —
   keine Tagesgrenzen-Rekonstruktion aus Timestamps mehr.

---

## D1 — Hinterfragt: `Symbol` als Primärschlüssel von `Asset` 🔴

**Probleme:**

- **Kollisionen sind real:** Symbole sind nur innerhalb einer Anlageklasse
  (und nicht mal dort) eindeutig. „EUR" existiert als Fiat und als Token; Aktien-
  Ticker („COIN", „ETH..." als ETN) kollidieren mit Crypto-Symbolen. Da alle
  Integrationen in denselben Symbolraum schreiben, würde z. B. ein Depot mit
  Ticker „BTC-ETF" neben Bitcoin funktionieren, aber „COIN" (Coinbase-Aktie)
  neben einem Token „COIN" nicht — der Zweitankömmling erbt Preis und Metadaten
  des Ersten.
- **Kein Rename möglich:** Symbol-Umbenennungen (kommt bei Coins vor) erfordern
  Kaskaden-Updates über drei Tabellen.
- **Case-Sensitivität:** Der Import schreibt Symbole, wie die Exchange sie liefert;
  Abfragen normalisieren teils mit `ToLower()` (`PortfolioQueryService`), teils nicht
  (`AssetService.GetAssetOrThrowAsync` vergleicht exakt). „btc" und „BTC" wären zwei Assets.

**Empfehlung:** Surrogate Key (`Guid Id`), Unique-Index auf `(Symbol, AssetType)`,
Symbol-Normalisierung (Uppercase) an genau einer Stelle beim Import. FKs in
`DailyHolding`/`AssetPriceHistory` auf `AssetId` umstellen. Das ist eine
größere Migration — lohnt sich aber, bevor mehr Anlageklassen (Stock/ETF ist ja
angelegt) ernsthaft genutzt werden.

## D2 — ~~Messungen als lose Zeitreihe mit Delete+Insert~~ ✅ erledigt 2026-07-11

Umgesetzt wie empfohlen: `DailyHolding` mit PK `(IntegrationId, Symbol, Date)`,
`Source {Sync, Manual}`, `RecordedAtUtc`; Import und manuelle Messungen nutzen
denselben idempotenten Upsert (kein Löschen mehr), Delete läuft über den
natürlichen Schlüssel. **Altdaten bewusst verworfen** statt migriert
(Single-User-Entscheidung; Messungen + Preishistorie per Migration geleert).
Bewusst offen gelassen: Intraday-Historie wäre ein eigenes Feature (separate
Tabelle mit Retention), kein Nebeneffekt. Der Name-Join der Config-Integrationen
existiert weiterhin → siehe D5 unten.

## D3 — Forward-Fill zur Abfragezeit, größtenteils gefixt 🟡

> **Teilstatus 2026-07-11:** Mess-/Holding-Query datumsbegrenzt und auf dem
> neuen PK unterwegs, Forward-Fill auf `maxfilldays` begrenzt, `AsNoTracking()`
> überall in der Aggregation. **Offen ist nur noch das Preiszeilen-Fenster.**

**Empfehlung (Rest):**

1. **Datumsfenster auch für Preise:** Für einen Bereich `[from, to]` werden nur
   benötigt: die Zeilen im Fenster **plus** je Asset die letzte
   Zeile vor `from` (als Startwert für den Fill). Letzteres ist in SQL ein
   `DISTINCT ON`/`ROW_NUMBER()`-Query — mit EF machbar
   (`GroupBy` + `Max` oder Raw SQL), Npgsql unterstützt `DISTINCT ON` via
   `EF.Functions`. Damit skaliert die Abfrage mit dem Fenster, nicht mit der
   Tabellengröße. Dazu Index `AssetPriceHistory (Symbol, Currency, Date DESC)`.

**Bewusst NICHT empfohlen:** Vorberechnete Tageswerte (Materialisierung von
`TotalValue`) — solange die Abfragen wie oben gefenstert sind, ist die Berechnung
on-the-fly billig, und man vermeidet Invalidierungsprobleme, wenn Preise oder
Messungen nachträglich korrigiert werden. Erst erwägen, wenn Jahre an Daten und
viele Nutzer zusammenkommen.

## D4 — Hinterfragt: Preis-Historie mit einem Preis pro Tag, Basiswährung hartkodiert 🟠

<a id="basiswaehrung"></a>

- ~~Basiswährung hartkodiert~~ ✅ erledigt 2026-07-09: `basecurrency` aus der
  Config, Frontend liest sie über `GET /api/config`.
- `AssetPriceHistory.Currency` im PK suggeriert Multi-Währungs-Fähigkeit, die
  nirgends existiert (die Abfragen filtern seit Bug 8 immerhin konsequent auf
  die Basiswährung). Entweder die Spalte konsequent nutzen (Umrechnung als
  Feature) oder sie entfernen und *eine* Basiswährung pro Installation
  festschreiben. **Empfehlung: Letzteres** — Umrechnung bei Bedarf über
  Fiat-Kurse zur Anzeigezeit, nicht über parallele Preisreihen. Achtung:
  Währungswechsel auf bestehender Installation invalidiert die Preishistorie
  (keine Umrechnung implementiert).
- **Rückwirkende Preise fehlen:** Ein heute angelegtes Asset hat nur ab heute
  Preise; historische Bestände davor werden mit Preis 0 bewertet (stille Nullen,
  `ApiHelper.cs:83`). CoinGecko (`/market_chart`) und Frankfurter (`/v1/{date}`)
  liefern Historie — ein Backfill-Job beim Anlegen eines Assets wäre ein großer
  Qualitätsgewinn und würde auch die Altdaten-Migration von
  [Bug 1](01-kritische-bugs.md) erledigen.
- Preis 0 als Fallback ist gefährlich unsichtbar: Ein Asset ohne Preiszeile
  drückt den Portfoliowert still. Die API sollte „Preis unbekannt" von
  „Preis = 0" unterscheiden (nullable Price im DTO + UI-Hinweis).

## D5 — Hinterfragt: Konfig-Integrationen vs. DB-Integrationen 🟠

Es gibt zwei Welten:

- **API-Integrationen** leben in `config.yml` (`CryptoTrackerIntegration`:
  Name, Type, Key, Secret, Passphrase) und werden beim Import per **Name** mit
  der DB-Tabelle `ExchangeIntegration` verheiratet (`UpdateService.cs:102`).
- **Manuelle Integrationen** leben nur in der DB (`IsManual = true`), angelegt
  über die UI.

Konsequenzen: Eine API-Integration hinzufügen erfordert Dateiedit + Neustart;
Umbenennen in der Config erzeugt eine neue DB-Integration und verwaist die alte
(inkl. ewigem Forward-Fill ihrer letzten Werte, siehe Bug 2); `Type`, `Key` usw.
sind in der DB unsichtbar, die UI kann eine API-Integration weder anzeigen noch
löschen.

**Empfehlung:** Integrationen vollständig in die DB (Spalten: `Type`,
verschlüsselte `Credentials`), CRUD über die API/UI, Config nur noch für
Bootstrap/Import bestehender Einträge. Das löst gleichzeitig S3 (Secrets),
macht `IsManual` zu `Type == Manual` und eliminiert den Name-Join.
Aufwand ≈ 2–3 PT, große UX-Verbesserung.

## D6 — Kleinere Modell-Punkte 🟡

- ~~`AssetMeasuring.Integration` non-nullable ohne `required`~~ ✅ 2026-07-11:
  `DailyHolding.Integration` nutzt das `= null!`-EF-Idiom, Warnung weg.
- `ExchangeIntegration.IsHidden` wird nirgends ausgewertet (Assets haben ein
  funktionierendes `IsHidden`, Integrationen nicht) — implementieren oder entfernen.
- `decimal(18,10)` global (`DatabaseContext.cs`): 8 Vorkommastellen reichen
  für BTC-Beträge, aber `TotalValue`-artige Summen in CHF können bei großen
  Portfolios knapp werden; für Preise von Micro-Cap-Coins sind 10 Nachkommastellen
  teils zu wenig (Preise < 1e-10 existieren). Pro Spalte entscheiden:
  Amounts `(38,18)`, Preise `(38,18)`, o. ä.
- `AssetType` enthält `Commodity`/`RealEstate`, für die es keinerlei Preis-Provider
  gibt — entweder mit „manueller Preis"-Feature hinterlegen oder streichen.
- Kein `CreatedAt`/`UpdatedAt` auf Asset/Integration/Preis-Tabellen — für
  Debugging von Import-Problemen hilfreich (`DailyHolding.RecordedAtUtc`
  existiert seit D2).

## Zielbild (Kurzfassung)

```
Asset:              Id (PK), Symbol, AssetType, UNIQUE(Symbol, AssetType), …   ← OFFEN (D1)
Integration:        Id (PK), Name, Type, CredentialsEncrypted, …               ← OFFEN (D5)
DailyHolding:       PK (IntegrationId, Symbol, Date), Amount, Source, RecordedAtUtc   ✅
AssetPrice:         PK (AssetId, Date), Price (Basiswährung)                   ← Currency-Spalte offen (D4)

Import:   fetch → validate → upsert vollständiger Tages-Snapshot (inkl. 0-Zeilen)   ✅
Lesen:    Fenster [from,to] + letzter Snapshot vor from, Fill max. N Tage   ✅ (Preis-Fenster offen, D3)
Zeit:     Speicherung UTC, Portfolio-Tag aus konfigurierter Zeitzone (PortfolioClock)   ✅
Währung:  baseCurrency aus Config, überall lowercase-normalisiert   ✅
```
