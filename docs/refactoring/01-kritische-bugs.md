# Kritische Bugs

Diese Punkte produzieren **falsche Zahlen oder Datenverlust** und sollten vor jedem
strukturellen Refactoring behoben werden. Sortiert nach Schweregrad.

---

<a id="bug-1"></a>
## Bug 1 — Fiat-Bewertung ist invertiert (falscher Wechselkurs) 🔴

> **Status 2026-07-09: Code-Fix umgesetzt** (Inversion in `FiatLogic`, Argument-Tausch in
> `YahooFinanceStockLogic`/`AlphaVantageStockLogic`, Kontrakt-Doku auf `AssetMetadata.Price`,
> Regressionstests in `cryptotracker.core.tests/Logic/FiatLogicTest.cs`).
> **Offen: Bereinigung der Altdaten** — bestehende Fiat-Zeilen in `AssetPriceHistory`
> sind weiterhin invertiert gespeichert; SQL siehe „Achtung Altdaten" unten.

**Betroffen:** `cryptotracker.core/Logic/CryptoTrackerAssetLogic.cs:163` → `FiatLogic.GetFiatsByIdsAsync`
(`cryptotracker.core/Logic/FiatLogic.cs:55`), Bewertung in `MessungDto.SumFromModels`
(`cryptotracker.database/DTOs/AssetMeasuringDto.cs:51`).

**Befund (verifiziert am 2026-07-09 gegen die Frankfurter-API):**

```
GET https://api.frankfurter.dev/v1/latest?base=CHF&symbols=EUR,USD
→ {"base":"CHF","rates":{"EUR":1.0844,"USD":1.2366}}
```

Die Rate bedeutet „1 CHF = 1.0844 EUR". `FiatLogic.GetFiatsByIdsAsync("chf", ["eur"])`
speichert aber `Price = 1.0844` für das Asset EUR, und die Bewertung rechnet
`TotalValue = Amount × Price`. Ein Bestand von 1000 EUR wird also mit
**1084 CHF** bewertet statt korrekt **~922 CHF** (= 1000 / 1.0844).

- EUR-Bestände: ~18 % zu hoch
- USD-Bestände: ~53 % zu hoch

**Gegenprobe im eigenen Code:** `YahooFinanceStockLogic.cs:53` macht es richtig herum —
dort wird `GetFiatByIdAsync(aktienWährung, zielWährung)` aufgerufen (Base = Fremdwährung,
Symbol = CHF), sodass die Multiplikation stimmt. Dieselbe API wird also an zwei Stellen
mit entgegengesetzter Semantik benutzt.

**Fix (klein):** In `CryptoTrackerAssetLogic.UpdateAllAssetMetadata` und
`AssetController` die Fiat-Preise als `1 / rate` speichern — oder `FiatLogic` so
umbauen, dass sie immer „Preis von X in Basiswährung" zurückgibt und
`YahooFinanceStockLogic` entsprechend anpassen. Zweiteres ist sauberer, weil dann
die Semantik von `AssetMetadata.Price` überall gleich ist: **„Wert von 1 Einheit
des Assets in der Basiswährung"**. Diese Semantik als XML-Doc an `AssetMetadata.Price`
dokumentieren.

**Achtung Altdaten:** Bereits gespeicherte `AssetPriceHistory`-Zeilen für Fiat-Assets
sind falsch. Migrationsskript oder einmaliges Neu-Berechnen einplanen (Frankfurter
liefert historische Kurse: `/v1/{date}`).

---

<a id="bug-2"></a>
## Bug 2 — Verkaufte/entfernte Assets zählen für immer weiter 🔴

> **Status 2026-07-09: behoben.** Import schreibt 0-Messungen für verschwundene
> Assets (Variante A, selbst-terminierend via `Amount != 0`-Filter); Forward-Fill
> ist auf `maxfilldays` (Config, Default 10) begrenzt (Variante B); 0-Positionen
> werden aus den Abfrage-Ergebnissen gefiltert. `ApiHelper` wurde dabei zum
> injizierbaren `PortfolioQueryService` (webapi/Services), inkl. Datums-Untergrenze
> in der Mess-Query (Teilfix des Skalierungsproblems aus 03/D3).
> Abgedeckt durch `PortfolioQueryServiceTest` (Fill-Grenze, 0-Filter, Batch).

**Betroffen:** `cryptotracker.webapi/Helpers/ApiHelper.cs:98` (`BuildDayResult`),
Zusammenspiel mit `UpdateService.Import`.

**Mechanik:**

1. Exchange-Abrufe liefern nur Bestände > 0 (`CryptoTrackerLogic.cs:362` filtert
   Coinbase auf `AvailableBalance > 0`, Binance auf `Total > 0`, Bitpanda auf
   `Balance > 0`). Wer sein BTC komplett verkauft, bekommt **keine** BTC-Zeile mehr.
2. `BuildDayResult` nimmt pro `(Symbol, Integration)` die **letzte Messung vor dem
   Stichtag — ohne jede Altersgrenze** (`groupMeasurings.FirstOrDefault(x => x.Timestamp < dayPlusOne)`).

Folge: Der letzte BTC-Stand vor dem Verkauf wird ab dann jeden Tag weiter angezeigt
und in den Gesamtwert eingerechnet — für immer. Dasselbe gilt für Integrationen,
die aus der Config entfernt wurden.

**Warum Forward-Fill überhaupt existiert:** Er überbrückt Tage ohne Import (Server
war aus, API down). Das ist legitim — aber er kann nicht zwischen „keine Daten"
und „Bestand ist 0" unterscheiden, weil der Import niemals Nullen schreibt.

**Fix-Optionen (Details und Empfehlung in [03](03-datenmodell-und-aggregation.md)):**

- **A (empfohlen):** Der Import schreibt pro Integration einen vollständigen
  Tages-Snapshot. Assets, die die Integration gestern noch hatte und heute nicht
  mehr, bekommen explizit eine 0-Messung. Forward-Fill bleibt für fehlende Tage
  erlaubt, weil ein vorhandener Snapshot dann immer vollständig ist.
- **B (zusätzlich sinnvoll):** Forward-Fill pro Integration begrenzen: nur füllen,
  wenn die letzte Messung der Integration nicht älter als N Tage ist (konfigurierbar),
  sonst als „stale" ausweisen statt stillschweigend einrechnen.

---

<a id="bug-3"></a>
## Bug 3 — Währungs-Casing „chf" vs. „CHF" 🟠

**Betroffen:**

- `ApiHelper.cs:39` filtert `AssetPriceHistory.Currency == "chf"` (klein)
- `CryptoTrackerAssetLogic.cs:161` schreibt `"chf"` (klein)
- `AssetController.cs:90` und `:163` schreiben `"CHF"` (groß)
- Tests seeden `"CHF"` (`WebApiTest.cs:67`)

Postgres vergleicht case-sensitiv. Ein via UI angelegtes Asset (`AddAsset` /
`SetExternalIdForSymbol`) bekommt eine Preiszeile mit Currency `"CHF"`, die die
Aggregation **nicht findet** — das Asset wird mit Preis 0 bewertet, bis der nächste
`UpdateService`-Lauf die Zeile auf `"chf"` normalisiert (via Currency-Mismatch-Zweig
in `UpdateMetadataForAsset:53`). Der Zweig existiert überhaupt nur, um diese
Inkonsistenz zu reparieren — ein Symptom-Workaround.

**Fix:** Eine Konstante bzw. Konfigwert `BaseCurrency` (normalisiert lowercase),
alle Literale ersetzen, Migration zum Normalisieren der Bestandsdaten, den
Currency-Mismatch-Zweig entfernen. Siehe auch [03 → Basiswährung](03-datenmodell-und-aggregation.md#basiswaehrung).

---

<a id="bug-4"></a>
## Bug 4 — Automatisch angelegte Assets sind immer `AssetType.Crypto` 🟠

**Betroffen:** `UpdateService.cs:120–125` (`AddMeasuring`).

Bitpanda liefert auch Fiat-Wallets (EUR, CHF, …) — `GetBitpandaFiatAccounts` wird
explizit abgerufen. Ein neues Asset „EUR" wird trotzdem als `Crypto` angelegt.
Folgen:

- Die Metadaten-Suche matcht „EUR" gegen die **CoinGecko-Coinliste per Symbol**
  (`CryptoTrackerAssetLogic.cs:92`). Es gibt Token mit Symbol „eur" — im besten
  Fall matcht nichts eindeutig (Asset bleibt ohne Preis), im schlechtesten Fall
  wird ein beliebiger Token gematcht und der EUR-Bestand mit einem Token-Preis bewertet.
- Der Nutzer muss den Typ manuell via `SetAssetTypeForSymbol` korrigieren, was
  wiederum verboten ist, sobald eine `ExternalId` gesetzt wurde (`AssetController.cs:137`).

**Fix:** `BalanceResult` um einen Typ-Hinweis erweitern (die Quelle weiß es:
Bitpanda-Fiat-Wallets sind Fiat, Frankfurter-Währungsliste kann als Fallback-Check
dienen). Beim Anlegen des Assets diesen Hinweis verwenden. Zusätzlich das
Symbol-basierte Auto-Matching nur ausführen, wenn es exakt einen Treffer gibt
**und** das Asset markiert werden kann („auto-matched, unbestätigt"), damit die
UI es zur Bestätigung anzeigen kann.

---

<a id="bug-5"></a>
## Bug 5 — Fehlgeschlagener Exchange-Abruf hinterlässt gelöschten Tag 🟠

> **Status 2026-07-09: behoben.** Exchange-Abrufe werfen bei API-Fehlern statt
> leere Listen zu liefern; Import holt erst und löscht/schreibt danach; Transaktion
> + try/catch pro Integration (inkl. `ChangeTracker.Clear()`); Metadaten-Import und
> `ExecuteAsync` sind separat abgesichert (StopHost-Gefahr). Integrationstyp ist
> jetzt ein Enum (`CryptoTrackerIntegrationType`, `Unknown` = Default).
> Abgedeckt durch `UpdateServiceTest` (0-Diff, Fehler-Isolation, Idempotenz).

**Betroffen:** `UpdateService.cs:61–81` (Import-Schleife) und die `Get*Accounts`-
Methoden in `CryptoTrackerLogic.cs`.

Der Import löscht **zuerst** die heutigen Messungen einer Integration und ruft
**danach** die Balances ab. Die Fehlerpfade der Exchange-Clients werfen aber nicht,
sondern loggen und geben **leere Listen** zurück (z. B. `GetKucoinAvailableAccounts:322`,
`GetBinanceAvailableAccounts:378`, `GetBitpandaAccounts:425`). Die Transaktion wird
also erfolgreich committet — mit gelöschten und nicht ersetzten Tagesdaten.

Dass es „meistens trotzdem funktioniert", liegt nur am Forward-Fill (Bug 2), der
den Vortagswert einspringen lässt. Sobald Forward-Fill korrekt begrenzt wird,
wird dieser Bug sichtbar.

**Fix:**
1. Reihenfolge umdrehen: erst Balances abrufen, nur bei Erfolg löschen + schreiben
   (bzw. Upsert, siehe [03](03-datenmodell-und-aggregation.md)).
2. Fehlerpfade müssen unterscheidbar sein: `GetAvailableIntegrationBalances` sollte
   bei API-Fehlern werfen oder ein Result-Objekt (`Success`/`Error`) zurückgeben —
   „leere Liste" ist als Fehlersignal ungeeignet, denn ein leeres Konto ist ein
   gültiger Zustand.
3. Pro Integration eine eigene Transaktion/Fehlerbehandlung, damit eine kaputte
   Integration nicht den Import aller anderen zurückrollt (aktuell: eine Exception
   irgendwo rollt alles zurück, `UpdateService.cs:92–97`).

---

<a id="bug-6"></a>
## Bug 6 — UTC/Lokalzeit-Mischung verschiebt Tagesgrenzen 🟠

**Betroffen (Auswahl):**

| Stelle | Zeitbasis |
|---|---|
| `UpdateService.cs:59` (Tages-Löschung) | `DateTime.UtcNow.Date` |
| `UpdateService.cs:134` (Messung) | `DateTime.UtcNow` |
| `CryptoTrackerController.cs:36,50,67,75` („heute") | `DateTime.Now` (Serverzeit) |
| `CryptoTrackerController.cs:29` | `date.ToLocalTime()` |
| `CryptoTrackerAssetLogic.cs:34,41` (Preis-Datum) | `DateTime.Now` |
| `MeasuringController.cs:47` (manuelle Messung) | `dto.Date.Date` (Client-Kind) |
| `ApiHelper.cs:50,78` (Tagesgrenzen) | `DateOnly` → UTC-Mitternacht |

Im Docker-Container läuft der Server auf UTC, lokal auf Europe/Zurich — dieselbe
Abfrage liefert je nach Umgebung andere „Tage". Eine Messung um 23:30 UTC gehört
lokal schon zum Folgetag; die Lösch-Logik des Imports (UTC-Tag) und die Anzeige-
Logik (lokaler Tag) schneiden unterschiedlich.

Zusätzlich ein Laufzeitrisiko: `MeasuringController.AddIntegrationMeasuring`
schreibt `dto.Date` direkt in eine `timestamptz`-Spalte. Npgsql wirft für
`DateTimeKind.Local`/`Unspecified` eine `InvalidCastException` — ob der Client ein
`Z`-Suffix schickt, entscheidet also über Erfolg oder 500er. **Verifizieren und
normalisieren** (`DateTime.SpecifyKind(..., Utc)` bzw. `ToUniversalTime()`).

**Fix-Prinzip:** Speicherung konsequent UTC; „Portfolio-Tag" ist eine fachliche
Entscheidung (empfohlen: konfigurierbare Anzeige-Zeitzone, Default Europe/Zurich)
und wird an genau einer Stelle aus UTC abgeleitet. `TimeProvider` injizieren statt
statischer `DateTime.Now`-Aufrufe — macht das auch endlich testbar.

---

## Bug 7 — Frankfurter-API-Host ist umgezogen 🟡

> **Status 2026-07-09: erledigt.** `FrankfurterCurrencyProvider` nutzt jetzt
> `https://api.frankfurter.dev/v1` als Default-BaseUrl (per Konstruktor-Parameter
> überschreibbar); Antwortformat der v1-Endpoints wurde live verifiziert.

~~`FiatLogic.cs:55,107` ruft `api.frankfurter.app` auf — das antwortet inzwischen
mit `301 Moved Permanently` auf `api.frankfurter.dev`. `HttpClient` folgt GET-
Redirects standardmäßig, es funktioniert also noch, kostet aber pro Aufruf einen
Roundtrip und bricht, falls der Redirect wegfällt. URL aktualisieren
(`https://api.frankfurter.dev/v1/...`).~~

## Bug 8 — `GetAsset` ignoriert die Währung des Preises 🟡

`AssetController.cs:46` nimmt die neueste `AssetPriceHistory`-Zeile **ohne
Currency-Filter**. Solange nur eine Währung geschrieben wird, geht das gut — mit
Bug 3 existieren aber zeitweise `chf`- und `CHF`-Zeilen, und sobald mehrere
Währungen unterstützt werden, ist der zurückgegebene Preis zufällig.

## Bug 9 — In-Memory-Caches ohne Ablauf in Singletons 🟡

`CryptoTrackerLogic._coinList` (`CryptoTrackerLogic.cs:479`) und
`FiatLogic._fiatList` (`FiatLogic.cs:101`) werden einmal pro Prozesslaufzeit
gefüllt und nie invalidiert. Neu gelistete Coins werden bis zum nächsten
Deployment/Neustart nicht gefunden. Zudem ist das Befüllen nicht threadsafe
(kein Lock; gleichzeitige Requests lösen parallele Downloads aus).
→ `IMemoryCache` mit TTL (z. B. 24 h) oder `Lazy<Task<T>>`-Pattern.

## Bug 10 — Seiteneffekt beim Rendern & nicht-reaktives Set im Dashboard 🟡

`cryptotracker.web/src/routes/+page.svelte:95–97`: Im `{#each}` wird
`{AddAsset(...)}` aufgerufen — ein Mutations-Seiteneffekt mitten im Template,
auf einem gewöhnlichen `Set` (nicht `$state`). Dass das Chart stimmt, ist
Zufall der Auswertungsreihenfolge; bei erneutem Rendern (Range-Wechsel)
akkumuliert das Set Assets über Abfragen hinweg. → Ableitung mit `$derived`
aus den geladenen Daten berechnen. Details in [06](06-frontend.md).
