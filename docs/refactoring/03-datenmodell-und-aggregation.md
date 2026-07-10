# Datenmodell & Aggregation

Das ist das Kernstück des Refactorings. Die Grundidee des Modells ist richtig
(Bestände und Preise getrennt, Wert zur Abfragezeit berechnet) — aber mehrere
Design-Entscheidungen verdienen es, hinterfragt zu werden.

## Ist-Zustand

```
Asset                    AssetMeasuring                AssetPriceHistory
─────                    ──────────────                ─────────────────
Symbol (PK)   ◄──────┐   Id (Guid, PK)                 (Symbol, Date, Currency) PK
ExternalId           ├── Symbol (FK)                   Symbol (FK) ─────► Asset
Name                 │   IntegrationId (FK) ──► ExchangeIntegration
Image                │   Timestamp (DateTime)          Date (DateOnly)
AssetType            │   Amount                        Currency
IsHidden             │                                 Price

ExchangeIntegration: Id (Guid PK), Name, Description, IsManual, IsHidden
```

Datenfluss:

1. `UpdateService` läuft alle `interval` Minuten, holt pro Config-Integration
   die Balances (via `IIntegrationProvider`), löscht die heutigen Messungen und
   schreibt die aktuellen Balances als neue Messungen mit `Timestamp = UtcNow`
   (inkl. 0-Zeilen für verschwundene Assets, siehe Bug 2).
2. `AssetMetadataService` schreibt/aktualisiert pro Asset genau eine
   Preiszeile pro Tag (Basiswährung aus Config, lowercase-normalisiert).
3. Lesend rekonstruiert `PortfolioQueryService.GetAssetDayMeasuringBatchAsync`
   für beliebige Tage den Bestand: pro `(Symbol, Integration)` die letzte
   Messung ≤ Stichtag, multipliziert mit dem letzten Preis ≤ Stichtag
   (Forward-Fill, begrenzt auf `maxfilldays`).

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
`AssetMeasuring`/`AssetPriceHistory` auf `AssetId` umstellen. Das ist eine
größere Migration — lohnt sich aber, bevor mehr Anlageklassen (Stock/ETF ist ja
angelegt) ernsthaft genutzt werden.

## D2 — Hinterfragt: Messungen als lose Zeitreihe mit Delete+Insert 🔴

**Ist:** Pro Import-Lauf werden die heutigen Messungen gelöscht und neu geschrieben
(`UpdateService.cs:63–67`). Bei `interval < 1 Tag` gibt es trotzdem nie mehr als
den letzten Lauf des Tages — die Intraday-Historie wird weggeworfen, aber die
Tabelle tut so, als wäre sie eine Timestamp-genaue Zeitreihe.

**Probleme:**

- Das Modell lügt: `Timestamp` suggeriert Intraday-Auflösung, faktisch ist es
  „ein Snapshot pro Tag, Uhrzeit = letzter Import". Die gesamte Leselogik
  (`ApiHelper`) muss deshalb aufwendig „letzte Messung im Tag" rekonstruieren.
- Delete+Insert erzeugt unnötige Write-Churn und instabile `Id`s.
- Die Lösch-Query filtert auf `Integration.Name == config.Name`
  (`UpdateService.cs:64`) — Name als Join-Kriterium, obwohl es eine Id gibt.
  Umbenennung in der Config erzeugt stillschweigend eine zweite Integration.
- Manuelle Messungen (`MeasuringController`) schreiben in dieselbe Tabelle mit
  derselben Ein-Zeile-pro-Tag-Semantik, aber eigener Upsert-Logik — zwei
  Implementierungen derselben Idee.

**Empfehlung — Snapshot-Modell ehrlich machen:**

```
AssetMeasuring (neu: DailyHolding)
────────────────────────────────
PK (IntegrationId, AssetId, Date)     -- natürlicher Schlüssel, DateOnly
Amount            decimal
RecordedAtUtc     DateTime            -- wann der Snapshot entstand (Audit)
Source            enum { Sync, Manual }
```

- Import macht ein **Upsert** pro `(Integration, Asset, Tag)` — idempotent,
  kein Löschen, keine Transaktions-Akrobatik.
- Der Import schreibt einen **vollständigen** Snapshot: Assets, die im letzten
  Snapshot der Integration vorkamen und jetzt fehlen, bekommen `Amount = 0`
  (hat [Bug 2](01-kritische-bugs.md) bereits gefixt; das Snapshot-Modell macht die 0-Zeilen-Logik strukturell überflüssig).
- Manuelle Einträge nutzen dieselbe Upsert-Semantik mit `Source = Manual`.
- Wer Intraday-Historie *will*, sollte das als bewusstes Feature bauen
  (separate Tabelle mit Retention), nicht als Nebeneffekt.

## D3 — Hinterfragt: Forward-Fill zur Abfragezeit, teils gefixt 🔴

> **Teilstatus 2026-07-09:** Die Mess-Query hat seit dem Bug-2-Fix eine untere
> Datumsgrenze und der Forward-Fill ist auf `maxfilldays` begrenzt (Punkt 2
> erledigt). Offen: Preiszeilen-Fenster, Indexe, `AsNoTracking()`.

**Ist:** `PortfolioQueryService.GetAssetDayMeasuringBatchAsync` lädt

- alle (nicht versteckten) Assets,
- alle Integrationen,
- **alle Preiszeilen** ≤ maxDay,
- Messungen im Fenster (seit Bug-2-Fix datumsbegrenzt) inkl. `Include(Integration)`

in den Speicher und rechnet dort. Für die Preiszeilen gibt es weiterhin keine
untere Datumsgrenze — die Grundlast wächst mit der Historie.

**Empfehlung (Rest):**

1. **Datumsfenster auch für Preise:** Für einen Bereich `[from, to]` werden nur
   benötigt: die Zeilen im Fenster **plus** je `(Integration, Asset)` die letzte
   Zeile vor `from` (als Startwert für den Fill). Letzteres ist in SQL ein
   `DISTINCT ON`/`ROW_NUMBER()`-Query — mit EF machbar
   (`GroupBy` + `Max` oder Raw SQL), Npgsql unterstützt `DISTINCT ON` via
   `EF.Functions`. Damit skaliert die Abfrage mit dem Fenster, nicht mit der
   Tabellengröße.
2. **Indexe:** Es gibt nur die automatischen FK-Indexe. Für die Leselast fehlen
   `AssetMeasuring (IntegrationId, Symbol, Timestamp DESC)` und
   `AssetPriceHistory (Symbol, Currency, Date DESC)`. Beim Umbau auf D2 wird der
   PK `(IntegrationId, AssetId, Date)` das größtenteils erledigen.
3. **`AsNoTracking()`** für alle Lese-Queries (Aggregation trackt aktuell
   tausende Entities völlig umsonst).

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

- `AssetMeasuring.Integration` ist non-nullable, aber ohne `required`/Konstruktor
  (`AssetMeasuring.cs:13`) — Compiler-Warnung wird unterdrückt statt modelliert.
- `ExchangeIntegration.IsHidden` wird nirgends ausgewertet (Assets haben ein
  funktionierendes `IsHidden`, Integrationen nicht) — implementieren oder entfernen.
- `decimal(18,10)` global (`DatabaseContext.cs:23`): 8 Vorkommastellen reichen
  für BTC-Beträge, aber `TotalValue`-artige Summen in CHF können bei großen
  Portfolios knapp werden; für Preise von Micro-Cap-Coins sind 10 Nachkommastellen
  teils zu wenig (Preise < 1e-10 existieren). Pro Spalte entscheiden:
  Amounts `(38,18)`, Preise `(38,18)`, o. ä.
- `AssetType` enthält `Commodity`/`RealEstate`, für die es keinerlei Preis-Provider
  gibt — entweder mit „manueller Preis"-Feature hinterlegen oder streichen.
- Kein `CreatedAt`/`UpdatedAt` auf irgendeiner Tabelle — für Debugging von
  Import-Problemen sehr hilfreich.

## Zielbild (Kurzfassung)

```
Asset:              Id (PK), Symbol, AssetType, UNIQUE(Symbol, AssetType), …
Integration:        Id (PK), Name, Type, CredentialsEncrypted, …   (keine Config-Dualität)
DailyHolding:       PK (IntegrationId, AssetId, Date), Amount, Source, RecordedAtUtc
AssetPrice:         PK (AssetId, Date), Price (Basiswährung der Installation)

Import:   fetch → validate → upsert vollständiger Tages-Snapshot (inkl. 0-Zeilen)
Lesen:    Fenster [from,to] + letzter Snapshot vor from, Fill max. N Tage, SQL-seitig
Zeit:     Speicherung UTC, Portfolio-Tag aus konfigurierter Zeitzone, TimeProvider injiziert
Währung:  baseCurrency aus Config, überall lowercase-normalisiert
```

Migrationsstrategie: neue Tabellen parallel anlegen, Bestandsdaten per Skript
überführen (letzte Messung pro Tag = Snapshot), Leselogik umstellen, alte
Tabellen nach Verifikationsphase droppen. Die EF-Migrationshistorie ist mit zwei
Migrationen jung genug, um das sauber zu machen.
