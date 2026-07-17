# Seiten & Flows

Phase 4 — hier wird das Redesign sichtbar. Phase 1–3 sind umgesetzt; alle
benötigten Primitives (inkl. `table`, `dialog`, `alert-dialog`, `toggle-group`)
liegen installiert bereit.

<a id="r11"></a>
## R11 — Dashboard (`routes/+page.svelte`)

Die wichtigste Seite: das ist der Screen, den man täglich kurz aufmacht.

**Befund:**

- **Die Hero-Karte ist eine leere Wüste.** `grid md:grid-cols-2 lg:grid-cols-4`
  mit **genau einer** Karte darin (Zeile 49-64) → auf Desktop steht der
  Portfoliowert in einem schmalen Kästchen links, daneben **drei Spalten
  Nichts**.
- **Der wichtigste Wert der App ist `text-2xl`** und hat **keinen Kontext**:
  `42318.55 CHF` — kein Delta, kein Vergleich, kein Trend. Man erfährt den Wert,
  aber nicht das, weswegen man die App öffnet: *Ist es mehr oder weniger als
  gestern?*
- **`selectedRange` ist an zwei Karten gebunden** (`bind:selectedRange`, Zeile
  84/97) — ändert man ihn in der einen, springt die andere mit. Das ist faktisch
  schon ein globaler Zeitraum, nur verkleidet als zwei lokale Switches. Verwirrend,
  weil es aussieht wie zwei unabhängige Regler.
- **Doppelte Datenbeschaffung:** `getStandingsByDay` und `getMeasuringsByDays`
  holen dasselbe — Standing **ist** die Summe der Measurings (siehe Backend
  `CryptoTrackerController.GetStandingByDay`). Zwei Requests, zweimal
  Backend-Arbeit, für einen Datensatz (F1).
- Fehlerfall rendert rohes `<p>{error.message}</p>` — dreimal (F3).
- `TrimMeasurings`-Probleme: siehe [R3.3](01-design-system.md#r3).

**Zielbild:**

```
┌──────────────────────────────────────────────────┐
│  ╔══════════════════════════════════════════╗    │  ← Gradient-Hero (E1),
│  ║  Portfolio                    [7D 30D 1Y]║    │    volle Breite
│  ║  42'318.55 CHF                           ║    │
│  ║  ▲ +984.20 (+2.4%)  ▁▂▃▅▇█▇▅      7 days ║    │  ← Delta + Sparkline
│  ╚══════════════════════════════════════════╝    │
│  ┌───────────────────┐ ┌───────────────────┐     │
│  │ History           │ │ Allocation        │     │
│  └───────────────────┘ └───────────────────┘     │
└──────────────────────────────────────────────────┘
```

1. **Hero über die volle Breite**, Gradient-Fläche, Wert in `text-5xl md:text-6xl`
   (proportionale Ziffern, [R2](01-design-system.md#r2)).
2. **Delta gegen den Zeitraum-Start**: absolut **und** relativ, mit Pfeil-Icon und
   Vorzeichen — **nie Farbe allein** ([R3.2](01-design-system.md#r3)). Delta-Farben
   sind ein eigener Satz, kein Serien-Grün/Rot.
   *Datenlage:* aus `getStandingsByDay` direkt ableitbar (erster vs. letzter Wert)
   — **kein neuer Endpoint nötig**.
3. **Ein globaler Zeitraum-Switch**, im Hero verankert, als `ToggleGroup`
   (7/30/90/365). Ersetzt die zwei `CardWithDays`-Regler und macht die
   bestehende Kopplung ehrlich sichtbar. In die URL (`?range=30`) → teilbar und
   überlebt Reload.
4. **Ein Load in `+page.ts`** statt drei `{#await}`. Standings aus den Measurings
   ableiten statt separat holen (F1) — spart einen Request.
5. **Tooltips + Empty-State.** Bei frischer Installation ohne Daten zeigt das
   Dashboard heute leere Charts; ein „Noch keine Daten — erste Integration
   verbinden" mit Button ist der bessere Einstieg.
6. Chart-Klick → Asset-Detail. Der Pie kann das schon (`PieChart:24-31`); der
   Line-Chart nicht — angleichen.

<a id="r12"></a>
## R12 — Manuelle Messungen: Wert direkt updaten 🟠

Explizit gewünscht (E3), und der Befund ist erfreulich.

**Der Fund:** `MeasuringService.AddIntegrationMeasuringAsync` ist **bereits ein
Upsert**:

```csharp
var holding = await _db.DailyHoldings.FindAsync(integration.Id, asset.Symbol, dto.Date);
if (holding == null) { holding = new DailyHolding { ... }; _db.DailyHoldings.Add(holding); }
holding.Amount = dto.Amount;   // ← überschreibt bestehenden Wert
```

**„Wert einfach updaten" braucht also keine Backend-Änderung und keinen neuen
Endpoint.** Derselbe `addIntegrationMeasuring`-Aufruf mit gleichem
`(Symbol, Date)` überschreibt den Betrag. Es fehlt **ausschliesslich die UI**.
Das passt auch zum Snapshot-Modell (PK `(IntegrationId, Symbol, Date)`,
[03/D2](../refactoring/03-datenmodell-und-aggregation.md)) — genau ein Wert pro
Asset und Tag.

**Befund UI** — der Flow ist heute in drei Seiten zersplittert:

| Route | Zustand |
|---|---|
| `integrations/[slug]` | Kacheln zeigen Beträge, **nicht editierbar** |
| `integrations/[slug]/add` | eigene Seite, drei nackte Felder, Full-Reload zurück |
| `integrations/[slug]/measurings` | **roher Text** — `{measuring.date} - {measuring.symbol}: {measuring.amount}` in einem `grid-cols-2`, daneben ein roter **„X"**-Button |

Die Verwaltungsseite ist der unfertigste Teil der App:
- Kein Titel-Markup, keine Tabelle, kein Styling — die Zeile hat sogar einen
  Typo im Klassennamen (`grid-tem`).
- **Löschen ohne Rückfrage.** Ein Klick auf „X", der Datensatz ist weg — kein
  Dialog, kein Undo, und bei Fehler passiert wieder nichts (`if (x.data)` ohne else).
- **Kein Editieren.** Ein falscher Betrag muss gelöscht und neu angelegt werden.
- `measurings.sort()` läuft **im Template** → sortiert bei jedem Re-Render neu
  und mutiert das Array (dieselbe Klasse wie Bug 10).

**Zielbild:** Die drei Seiten auf **eine** zusammenführen.

```
Ledger                                     [+ Holding]
┌──────────────────────────────────────────────────────┐
│ Date         Asset        Amount                     │
│ 16.07.2026   ● BTC        0.54120000   [✎]  [🗑]     │
│ 16.07.2026   ● ETH        3.20000000   [✎]  [🗑]     │
│ 15.07.2026   ● BTC        0.54120000   [✎]  [🗑]     │
└──────────────────────────────────────────────────────┘
```

(Datumsformat bleibt `de-CH` — Sprache ≠ Zahlenformat, siehe [R8](02-fundament.md#r8).)

- **Tabelle** (`table` aus der Registry) statt Rohtext, sortiert nach Datum
  absteigend — **im Script**, nicht im Template. Beträge `tabular-nums`.
- **Inline-Edit:** Klick auf `✎` macht die Betragszelle zum `Input`; Enter
  speichert per `addIntegrationMeasuring` (= Upsert), Escape bricht ab. Beim
  Speichern Zelle disablen, nach der Antwort aktualisieren + Toast
  ([R6](02-fundament.md#r6)). *Bewusst kein* optimistisches Update mit Rollback —
  gegen die lokale Single-User-API dauert der Request Millisekunden, die
  Rollback-Maschinerie wäre Komplexität ohne spürbaren Gewinn.
  **Das ist der ganze „Wert updaten"-Wunsch — reine Frontend-Arbeit.**
- **Löschen mit Bestätigung** (`alert-dialog`) statt scharfem X.
- **„+ Messung" als Dialog** statt eigener Route — nach dem Speichern bleibt man
  in der Tabelle und sieht die neue Zeile. Der häufige Fall ist „mehrere
  Messungen am Stück erfassen"; heute kostet jede davon zwei Full-Reloads.
  Datum vorbelegt auf heute (macht `add/+page.svelte:11` schon richtig), Asset
  per Combobox ([R5.1](02-fundament.md#r5)).
- **Nur bei `isManual`** — Auto-Integrationen bleiben read-only (das Backend
  wirft sonst `InvalidOperationException`, `MeasuringService:27`; die UI blendet
  die Aktionen heute schon korrekt aus, `integrations/[slug]/+page.svelte:68`).

<a id="r13"></a>
## R13 — Restliche Seiten

### Assets (`assets/`)
Das Kachel-Grid ist gut — **bleibt**. Verbesserungen:
- Kacheln zeigen nur Symbol/Name. **Wert und Bestand fehlen** — auf der
  Vermögensseite die naheliegendste Information. Anreichern (Kurs, Bestand,
  Delta) — Daten liefert `getLatestMeasurings` bereits.
- Empty-State, wenn noch kein Asset da ist.
- ~~`assets/add`: auf Combobox + Primitives~~ ✅ Phase 2 (ETF wurde entfernt
  statt deaktiviert — war nie speicherbar).

### Integrationen (`integrations/`)
- Kacheln: `bg-amber-100`/`bg-blue-100` für Manuell/Automatisch sind
  **hartkodiert an den Tokens vorbei** (`IntegrationTiles.svelte:27-29`) →
  `badge` aus der Registry. *Die Detailseite ist seit Phase 3 schon auf
  `Badge` + `PageHeader` — nur die Kacheln fehlen noch.*
- Die Initialen-Kreise (`name.slice(0,2)`) sind in Kacheln und Detail-Header
  dupliziert → `<IntegrationAvatar>`.
- Kachel zeigt keinen Wert und **kein Datum des letzten Syncs** — bei
  Auto-Integrationen die wichtigste Vertrauensinformation („läuft das noch?").
  **Entschieden (E10): wird gebaut.** `IntegrationDto` (geprüft: hat heute nur
  Id/Name/Description/IsHidden/IsManual) bekommt ein `LastSyncedAtUtc` =
  max. `RecordedAtUtc` der `DailyHoldings` der Integration — eine Subquery im
  Service, danach `make api` für den TS-Client. Anzeige relativ („vor 2 h"),
  bei Manuell-Integrationen als „letzte Messung". **Der einzige Backend-Punkt
  des gesamten Plans.**

### Report (`report/`) — bleibt als Stichtags-Bericht (E8)

**Entschieden:** Report bleibt eine eigene Seite (Anwendungsfall:
Stichtags-Blick, z. B. Bestände per Jahresende) und bekommt das volle Redesign
statt nur Kosmetik.

Befunde am Ist-Zustand:
- `text-gray-500` hartkodiert, `toFixed(2)`/`toFixed(8)` → [R8](02-fundament.md#r8).
- Filtert `isHidden` client-seitig (Zeile 45), obwohl der Endpoint versteckte
  Assets bereits ausschliesst (geprüft: `PortfolioQueryService:35`) — doppelte
  Logik, die Client-Seite fliegt raus (F6).
- `data: api.AssetHoldingDto[] | null` ohne Initialisierung → `undefined ≠ null`.
- `date.toISOString()` an `getMeasuringsByDate`: Endpoint sollte auf
  `DateOnly` (`yyyy-MM-dd`) umgestellt werden (F2).
- **Es gibt keinerlei Datums-UI.** Die Navbar verlinkt nur `/report` (= heute);
  das Datum kommt ausschliesslich aus `?date=` in der URL, und **nichts in der
  App erzeugt so einen Link** (geprüft). Die Kernfunktion — ein anderes Datum
  ansehen — erreicht man nur durch URL-Tippen.

Zielbild:
- **Datums-Navigation:** Date-Picker + Vor-/Zurück-Tasten (Tag), Default heute;
  Datum bleibt in der URL (teilbar, Reload-fest — wie der Zeitraum in R11).
- **Tabelle statt Kachel-Grid** (`table`): Symbol, Bestand, Kurs, Wert, Anteil %
  — `tabular-nums`, sortierbar nach Wert. Ein Bericht ist eine Tabelle, kein
  Kachelfeld; das ist zugleich die Tabellenansicht, die die Palette-Auflagen
  ([R3.2](01-design-system.md#r3)) sowieso fordern.
- Summenzeile = Portfoliowert am Stichtag.
- Empty-State für Tage ohne Daten (heute: nackter Text „Keine Daten vorhanden").

### Info (`info/`) — ✅ gelöscht (E9, Phase 3)
Seite samt Nav-Eintrag entfernt.

## Definition of Done (Phase 4)

- [ ] Dashboard beantwortet „mehr oder weniger als gestern?" ohne Nachdenken
- [ ] Ein globaler Zeitraum-Switch, in der URL
- [ ] Kein leerer 3-Spalten-Raum mehr neben dem Hero
- [ ] Messwert lässt sich inline korrigieren, ohne Löschen+Neuanlegen
- [ ] Löschen fragt nach
- [ ] Mehrere Messungen am Stück erfassbar, ohne Full-Reload
- [ ] Report hat Datums-Navigation und Tabelle mit Summenzeile (E8)
- [ ] Integrations-Kacheln zeigen den letzten Sync (E10)
- [x] `/info` + Nav-Eintrag gelöscht (E9)
