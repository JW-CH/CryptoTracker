# Design-System

Fundament für [E1 „Expressiv, mit Charakter"](README.md#getroffene-entscheidungen).
Alles hier ist Phase 1 — ohne diese Tokens malt jede Seite wieder ihre eigene Suppe
(wie heute `border-gray-200` in acht Dateien).

<a id="r1"></a>
## R1 — Dark-Mode überhaupt erst einschalten 🟠

**Befund:** `src/app.css:42-74` definiert eine vollständige `.dark`-Palette — 32
Tokens, sauber gepflegt. `@custom-variant dark (&:is(.dark *))` ist konfiguriert.
Und dann setzt **nirgends im Code jemand die Klasse `dark`**. Das gesamte
Dark-Design ist toter Code. Es gibt keinen Toggle, keine
`prefers-color-scheme`-Auswertung, nichts.

Das ist der grösste Gewinn pro Aufwand im ganzen Plan: Das Design existiert schon,
es muss nur erreichbar werden.

**Zielbild** — drei Teile:

1. **Store** `src/lib/stores/theme.ts`: `'light' | 'dark' | 'system'`, persistiert
   in `localStorage`, Default `'system'` (via `matchMedia('(prefers-color-scheme: dark)')`).
2. **Anti-FOUC-Snippet** in `src/app.html`, **blockierend im `<head>`** — vor dem
   ersten Paint, sonst blitzt bei jedem Reload Weiss auf:
   ```html
   <script>
     const t = localStorage.getItem('theme');
     const dark = t === 'dark' || (!t || t === 'system') &&
       matchMedia('(prefers-color-scheme: dark)').matches;
     document.documentElement.classList.toggle('dark', dark);
   </script>
   ```
   Zusätzlich `color-scheme: light dark` setzen, damit native Controls
   (Datepicker, Scrollbars) mitziehen — sonst bleibt der `<input type="date">`
   in `integrations/[slug]/add` ein weisser Klotz im dunklen Layout.
3. **Toggle** in der Navbar ([R10](03-navigation-und-layout.md#r10)).

**Wichtig:** `.dark`-Klasse auf `<html>`, nicht `<body>` — die Variante
`&:is(.dark *)` matcht Nachfahren, `<html>` deckt am meisten ab.

> **Vorher aufräumen:** Die hartkodierten Farben (`bg-gray-200` in Skeletons,
> `border-gray-200`/`blue-500` in allen Formularen, `text-gray-500` im Report)
> ignorieren Tokens und werden im Dark-Mode **sofort sichtbar kaputtgehen**.
> R1 macht sie von unsichtbarem Schmutz zu einem echten Bug — deshalb gehört
> [R5](02-fundament.md#r5) direkt daneben.

<a id="r2"></a>
## R2 — Brand-Akzent & Typografie

**Befund:** `--primary` ist heute `oklch(0.208 0.042 265.755)` — ein
Fast-Schwarz mit Hauch Blau, der shadcn-Default. Korrekt, aber vollkommen
charakterlos: die App sieht aus wie jedes andere shadcn-Starter-Projekt.

**Vorschlag: Violett als Brand-Hue.** Passt zum Krypto-Kontext ohne Bitcoin-Orange-
Klischee, funktioniert in beiden Modi, und kollidiert nicht mit den
Delta-Farben Grün/Rot (die im Portfolio-Kontext für Gewinn/Verlust **reserviert**
sind — deshalb scheidet ein grüner oder roter Brand-Akzent aus).

**Standard-konformer Weg: `--primary` überschreiben, keine neuen Token-Namen
erfinden.** shadcn-Komponenten sind alle gegen `--primary`/`--ring` verdrahtet
(`button.svelte:10` → `bg-primary text-primary-foreground`). Wer `--brand`
danebenstellt, muss jede Komponente anfassen und verlässt den Standard; wer
`--primary` umsetzt, brandet **die ganze App mit drei Zeilen** — Buttons,
Fokus-Ringe, aktiver Nav-State ziehen automatisch mit. Genau dafür ist das
Token-System da.

| Token | Light | Dark | Wirkt auf |
|---|---|---|---|
| `--primary` | `oklch(0.433 0.167 284)` | `oklch(0.67 0.145 287)` | Primär-Buttons, aktiver Nav-State |
| `--primary-foreground` | `oklch(0.98 0 0)` | `oklch(0.18 0.03 285)` | Text auf Primär-Fläche |
| `--ring` | wie `--primary` | wie `--primary` | Fokus-Ring (heute neutralgrau) |

(Das sind exakt `#4a3aa7` light / `#9085e9` dark — dieselben Werte wie Slot 7
der validierten Chart-Palette, nachgerechnet statt gerundet. In `oklch` notiert,
weil `app.css` durchgängig `oklch` verwendet.)

`baseColor: slate` in `components.json` bleibt: die neutralen Flächen sind gut,
nur der Akzent bekommt Charakter.

> **Entschieden (E7):** Violett ist der Brand-Hue, und **Violett fällt aus der
> Serienpalette** — der Brand-Akzent darf keine Serienfarbe sein, sonst sieht
> eine Chart-Serie aus wie ein Button. Konsequenz: die Palette wird 7-slotting
> und musste **neu geordnet und revalidiert** werden, siehe [R3.2](#r3).

**Typo (E1: „grössere Typo"):** Keine Custom-Webfont — `system-ui` bleibt. Der
Charakter kommt aus der *Skala*, nicht aus der Schriftart; eine Webfont kostet
Ladezeit und CLS für wenig Ertrag.

- Hero-Zahl (Portfoliowert): `text-5xl md:text-6xl font-bold tracking-tight`
- Seiten-`h1`: `text-2xl font-bold tracking-tight` (wie heute — bleibt)
- **Alle Beträge in Tabellen/Achsen: `tabular-nums`.** Ohne das springen die
  Ziffern beim Live-Update. Freistehende Hero-Zahlen dagegen mit
  Proportionalziffern — die sehen so besser aus.

**Gradient-Hero (E1):** Genau **eine** Gradient-Fläche in der App — die
Portfolio-Karte am Dashboard. Ein Gradient, der überall klebt, ist kein Akzent
mehr, sondern Rauschen.

> ⚠️ **Auf dem Gradient gilt die Palette nicht.** Die Chart-Palette und die
> Delta-Farben sind gegen die *Kartenflächen* validiert (Weiss/`#1d283a`) —
> **nicht** gegen einen violetten Gradient. Alles, was im Hero liegt (Delta,
> Sparkline, Zeitraum-Switch), wird in `--primary-foreground`/Weiss mit
> Opazitätsstufen gezeichnet, nie in Delta-Grün/Rot oder Serienfarben.
> Steigen/Fallen trägt dort **allein der Pfeil + Vorzeichen** — was ohnehin
> Pflicht ist, weil Farbe nie alleiniger Träger sein darf ([R3.2](#r3)).

<a id="r3"></a>
## R3 — Charts: Bibliothek, Palette, stabile Farbe 🟠

Das hier ist der inhaltlich dichteste Punkt des Redesigns.

### R3.1 — Chart.js ablösen: **LayerChart**

**Befund:** Beide Chart-Komponenten haben dieselben strukturellen Probleme:

- **Sie setzen keinerlei Palette.** `PieChart.svelte:36-45` und
  `LineChart.svelte:26-33` übergeben nur `data` — die Farben kommen von
  Chart.js-Defaults. Die schön gepflegten `--chart-1..5`-Tokens in `app.css:27-31`
  sind **komplett unbenutzt**.
- **Sie sind nicht reaktiv.** `data` wird einmal beim Init berechnet, `new Chart()`
  läuft in `onMount`. Ändern sich die Props, passiert nichts.
- **Sie werden nie zerstört.** Kein `chart.destroy()` in `onDestroy` → jeder
  Zeitraum-Wechsel am Dashboard lässt eine Chart-Instanz samt Listener liegen.
  **Das ist ein Memory-Leak**, heute maskiert davon, dass `{#await}` die Komponente
  neu mountet.
- `generateGUID()` + `document.getElementById(id)` ist eine DOM-Krücke um Svelte
  herum — `bind:this` gibt es geschenkt. (`generateGUID` nutzt zudem `Math.random`,
  siehe F4.)

**Der eigentliche Killer ist aber [R1](#r1):** Chart.js zeichnet auf **Canvas**.
Canvas kann **keine CSS-Variablen lesen**. Sobald der Theme-Toggle existiert,
müsste jeder Chart bei jedem Toggle seine Farben in JS neu auflösen und sich neu
aufbauen — Achsen, Labels und Gridlines inklusive, sonst bleibt dunkelgraue
Schrift auf dunklem Grund. Man baut also eine Farb-Plumbing-Schicht, die ein
SVG-Chart geschenkt bekommt.

**Empfehlung: [LayerChart](https://layerchart.com) 2.0.1** (`peerDependencies:
svelte ^5.0.0` — geprüft am 2026-07-16).

| | Chart.js (heute) | LayerChart |
|---|---|---|
| Rendering | Canvas | **SVG** → CSS-Variablen wirken direkt |
| Theme-Toggle | Chart-Neuaufbau in JS nötig | **kostenlos**, CSS regelt |
| Reaktivität | manuell (`update()`) | Svelte-5-deklarativ, `$state` reicht |
| Cleanup | `destroy()` von Hand | Komponenten-Lifecycle |
| Tooltips | Plugin-Config | eingebaut |
| Passung | framework-agnostisch | **Svelte-nativ; ist die Chart-Basis von shadcn-svelte** — genau die Sprache, die das Projekt schon spricht |

Geprüfte Alternativen, verworfen: **Unovis** (`@unovis/svelte` 1.6.7 deklariert als
Peer nur `svelte ^3.48 || ^4` — kein Svelte 5), **svelte-echarts** (1.0.0 will
`echarts ^5`, aktuell ist 6.1.0 → Peer-Konflikt).

Zwei Punkte, die den Einstieg weiter verbilligen bzw. die Phasen-Zuordnung
begründen:

- **Die Registry liefert die Wrapper mit:** shadcn-svelte hat eine
  `chart`-Komponente (gegen den Registry-Index geprüft) — Container,
  Tooltip-Styling und Token-Anbindung auf LayerChart-Basis kommen also per
  `add chart`, nicht aus der Tastatur.
- **Warum der Tausch in Phase 1 gehört und nicht zu R11 (Phase 4):** Der
  Theme-Toggle ([R1](#r1)) shippt in Phase 1/3. Ab dann wären Chart.js-Charts
  im Dark-Mode halb unleserlich (dunkelgraue Default-Achsen auf dunkler Karte).
  Der Tausch muss **vor** dem Toggle live gehen — deshalb Phase 1, als
  1:1-Ersatz hinter unveränderter Prop-Schnittstelle; Tooltips/Animation/Klick
  kommen erst mit R11.

**Ehrlicher Trade-off:** LayerChart ist deklarativer und *tiefer* — man baut
Achsen/Tooltips aus Bausteinen, statt ein Options-Objekt zu füllen. Für die zwei
Chart-Typen hier ist das ein Nachmittag Einarbeitung. SVG ist bei sehr vielen
Punkten langsamer als Canvas; bei ≤ 365 Tagespunkten × ≤ 8 Serien ist das
irrelevant. Und: LayerChart 2.x ist jung — bei einem Blocker ist der Rückweg zu
Chart.js offen, weil beide Charts ohnehin hinter denselben zwei Komponenten
liegen. **Genau deshalb bleibt die Prop-Schnittstelle (`labels`, `datasets`,
`values`) unverändert** — der Tausch ist dann auf zwei Dateien begrenzt.

### R3.2 — Palette als Tokens, **validiert**

**7 Slots** — Violett gehört seit E7 dem Brand und ist raus. Wichtig: nach dem
Entfernen musste die Folge **neu geordnet** werden. In der alten Reihenfolge
wären Orange und Rot benachbart geworden (ΔE 7.1 normal — unter dem harten
Floor von 15); ein Brute-Force über alle 720 Ordnungen ergab, dass nur **6**
davon beide Modi bestehen. Das ist die beste:

> **Revidiert 2026-07-17:** Der Brand-Hue ist inzwischen **Teal** (Preset
> `b5dx81JH0`, siehe R2) statt Violett. Konsequenz für die Palette: **Aqua fällt
> raus** (zu nah am Teal-Umfeld ist er nicht — aber Violett ist wieder frei und
> hebt die schwächste Nachbartrennung von ΔE 7.2 auf **13.0**, aus dem Floor-Band
> in einen sauberen Pass). Neu geordnet und gegen die Stone-Flächen
> (`#ffffff`/`#1c1917`) revalidiert:

> **Revidiert 2026-07-17 (final):** Mit dem Teal/Stone-Preset (R2) wirkte jede
> bunte Kategorial-Palette wie ein Fremdkörper. Entscheidung: Es gilt der
> **Preset-Standard — die monochrome Teal-Skala** (Slots 1–5 = Preset, 6/7 =
> Teal-200/900 als Erweiterung), identisch in Light und Dark. **Bewusster
> Trade-off:** Nachbarn unterscheiden sich nur über Helligkeit, nicht über den
> Farbton — Identität tragen deshalb Legende, Tooltips und Direktlabels, die
> ohnehin Pflicht sind. Die formale CVD-Validierung wurde dafür bewusst
> ausgesetzt.

| Slot | Teal-Stufe | Wert (beide Modi) |
|---|---|---|
| 1 | 300 | `oklch(0.855 0.138 181.071)` |
| 2 | 500 | `oklch(0.704 0.14 182.503)` |
| 3 | 600 | `oklch(0.6 0.118 184.704)` |
| 4 | 700 | `oklch(0.511 0.096 186.391)` |
| 5 | 800 | `oklch(0.437 0.078 188.216)` |
| 6 | 200 | `oklch(0.91 0.096 180.426)` |
| 7 | 900 | `oklch(0.386 0.063 188.416)` |

Geprüft gegen die **tatsächlichen** Kartenflächen des Projekts (Light `#ffffff`
= `--card`, Dark `#1d283a` = `--card` dark): Lightness-Band, Chroma-Floor und
Normal-Sicht-Floor (ΔE 19.3) bestehen; die **CVD-Trennung liegt mit ΔE 7.2 im
Floor-Band 6–8** — schwächer als die 8er-Palette (9.1/8.4) und nur zulässig mit
Zweitkodierung. Das ist der messbare Preis von E7.

**Die Reihenfolge ist keine Deko, sondern der Sicherheitsmechanismus** — sie
maximiert den minimalen Abstand benachbarter Slots unter simulierter
Farbenblindheit. Slots **in dieser Reihenfolge vergeben, niemals umsortieren, niemals
zyklisch wiederverwenden.**

Auflagen aus der Validierung (durch das Floor-Band jetzt **verbindlicher**):
- **Zweitkodierung ist Pflicht:** Direktlabels bzw. Tooltips mit Namen + die
  Tabellenansicht — Farbe darf nirgends die einzige Identitätsquelle sein.
  (Wäre wegen der Kontrast-WARNs — Magenta/Gelb/Aqua unter 3:1 auf Weiss,
  Grün exakt 3.0 auf Dark — ohnehin nötig gewesen.)
- Als `--chart-1..7` in `app.css` ablegen (die bestehenden 1..5 ersetzen) — dann
  greifen sie über CSS auch im SVG von LayerChart.
- Grün/Rot sind hier **Serienfarben**; die **Delta-Farben** für Gewinn/Verlust
  ([R11](04-seiten-und-flows.md#r11)) sind ein anderer Satz (`#006300`/`#0ca30c`
  bzw. `#d03b3b`). Ein Delta-Grün darf nie wie Serie 2 aussehen → Delta immer
  **mit Pfeil-Icon und Vorzeichen**, nie Farbe allein.

### R3.3 — Farbe muss an das Asset gebunden sein, nicht an den Rang 🟠

**Der schwerwiegendste Chart-Befund.** Heute vergibt Chart.js Farben nach
Array-Index. `TrimMeasurings` (`routes/+page.svelte:29-45`) sortiert aber
**nach Wert absteigend**. Folge:

- **Steigt ETH über BTC, tauschen beide die Farbe.** Der Nutzer sieht eine
  Umfärbung und denkt an einen Datenfehler.
- Dieselbe Münze hat **im Pie eine andere Farbe als im Line-Chart**, weil die
  Index-Reihenfolge dort eine andere ist.
- Farbe kodiert damit *Rang*, nicht *Identität* — sie trägt keine Information,
  sondern erfindet welche.

**Zielbild:** eine reine Funktion `colorForSymbol(symbol, allSymbols)` in
`$lib/charts/palette.ts`. Slot-Zuweisung über eine **stabile** Ordnung —
alphabetisch nach Symbol oder eine fixierte Asset-Reihenfolge — **nicht** über die
Wert-Sortierung. `'Other'` bekommt einen **fixen neutralen Grauton** ausserhalb
der sieben Slots (es ist keine Entität, sondern ein Sammelbecken) und immer den
letzten Platz. Beide Charts ziehen aus derselben Funktion. Als reine Funktion ist
sie zudem trivial testbar (F4 wünscht sich genau das).

**Nebenbefunde in `TrimMeasurings`:**
- `data.sort()` sortiert **in place** → mutiert die Response-Daten (F1).
- Die Funktion wird **pro Chart zweimal** aufgerufen (Zeile 76/77 — einmal für
  Labels, einmal für Values), sortiert also zweimal.
- `summarize` ist `let summarize = true` — **kein `$state`, nie verändert**.
  Toter Schalter (F1). Entweder echter Toggle in der UI oder weg.
- Die Top-7-Grenze passt nach E7 **exakt**: 7 farbige Slots + graues „Other".
  Beibehalten.
- **Der Zusammensetzungs-Line-Chart trimmt heute gar nicht:** `UniqueSymbols`
  zeichnet **alle** Assets als Serien — beim 9. Asset gehen der Palette die
  Slots aus. Dieselbe Top-7+„Other"-Logik muss auch dort greifen (mit R11).

<a id="r4"></a>
## R4 — Motion

E1 nennt „animierte Charts". Regeln, damit das nicht kippt:

- **Chart-Einblendung:** Line-Chart wächst über ~400 ms von links, Pie-Segmente
  über ~300 ms. **Nur beim ersten Render**, nicht bei jedem Daten-Update — sonst
  tanzt das Dashboard bei jedem Zeitraum-Wechsel.
- **Werte-Übergänge:** Zahlen-Tweens (`svelte/motion`) für die Hero-Zahl sind
  hübsch, aber **nur bei echtem Wertwechsel**, nicht beim Laden.
- **Hover:** Die `-translate-y-0.5`-Kachel-Hovers sind gut und bleiben.
- **`prefers-reduced-motion: reduce` respektieren** — alle Chart-Animationen und
  Tweens auf 0. Nicht verhandelbar, ist ein Accessibility-Thema.

## Definition of Done (Phase 1)

- [ ] Theme-Toggle schaltet Light/Dark/System, kein FOUC beim Reload
- [ ] `--brand`-Tokens gesetzt; Brand-vs-Serienfarben-Kollision entschieden
- [ ] `--chart-1..7` in `app.css` (neue E7-Ordnung), `--chart-1..5` ersetzt
- [ ] `colorForSymbol()` existiert, beide Charts nutzen sie; **BTC hat in Pie und
      Line dieselbe Farbe und behält sie, wenn sich der Rang ändert**
- [ ] Charts überleben einen Theme-Toggle ohne Neuaufbau (Achsen/Labels lesbar)
- [ ] Kein `bg-gray-200`/`border-gray-200` mehr im Code (→ [R5](02-fundament.md#r5))
