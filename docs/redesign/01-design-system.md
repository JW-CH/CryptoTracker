# Design-System — ✅ umgesetzt (Phase 1, 2026-07-17)

Kurzprotokoll; die ursprünglichen Befunde sind erledigt und gestrichen.
Referenzmaterial für Phase 4 (Palette, Typo, Gradient-Regeln) bleibt unten stehen.

<a id="r1"></a>
## R1 — Dark-Mode ✅

- Theme-Store `$lib/stores/theme.svelte.ts` (Svelte-5-Runes): `light | dark |
  system`, localStorage, `matchMedia`-Listener, `resolved`-Getter (u.a. für den
  Toaster).
- Anti-FOUC-Snippet blockierend im `<head>` von `app.html` — muss mit dem Store
  deckungsgleich bleiben (gleicher Key, gleiche `system`-Auflösung).
- Toggle als Sonne/Mond-Dropdown (Light/Dark/System) in der Navbar — aus R10
  vorgezogen.

<a id="r2"></a>
## R2 — Brand & Typografie ✅ (revidiert: Teal statt Violett)

**Umgesetzt via shadcn-Preset `b5dx81JH0`** (Style „Luma", Neutrals **Stone**,
Akzent **Teal**), angewendet mit der shadcn-svelte-CLI — sie hat auch alle
installierten UI-Komponenten neu generiert (`components.json`: `style: luma`,
`baseColor: stone`).

- `--primary`: Teal-700 light / Teal-800 dark; `--ring` neutral (Stone) — dem
  Preset gefolgt statt der alten „Ring = Primary"-Empfehlung.
- **Abweichung vom Plan:** Das Preset bringt **Inter Variable** als Font mit
  (self-hosted via `@fontsource-variable/inter` — kein externer Request). Die
  ursprüngliche „keine Webfont"-Empfehlung ist damit überholt.
- Die frühere Violett-Entscheidung (E7) ist obsolet; Violett-Werte tauchen
  nirgends mehr auf.

**Typo-Skala (Referenz für Phase 4):**

- Hero-Zahl (Portfoliowert): `text-5xl md:text-6xl font-bold tracking-tight`,
  proportionale Ziffern
- Seiten-`h1`: `text-2xl font-bold tracking-tight` (via `<PageHeader>`)
- Beträge in Tabellen/Achsen: **`tabular-nums`** — sonst springen Ziffern beim
  Update

**Gradient-Hero (Referenz für R11):** Genau **eine** Gradient-Fläche in der App —
die Portfolio-Karte am Dashboard, auf Teal-Basis.

> ⚠️ **Auf dem Gradient gilt die Palette nicht.** Alles im Hero (Delta,
> Sparkline, Zeitraum-Switch) wird in `--primary-foreground`/Weiss mit
> Opazitätsstufen gezeichnet, nie in Delta- oder Serienfarben. Steigen/Fallen
> trägt dort allein Pfeil + Vorzeichen.

<a id="r3"></a>
## R3 — Charts: Chart.js → LayerChart ✅

- `PieChart`/`LineChart` neu auf LayerChart (SVG → Tokens wirken direkt,
  Theme-Toggle gratis, Svelte-5-reaktiv, kein Memory-Leak mehr). Chart.js
  deinstalliert.
- Pie: Tooltip, Segment-Klick → Asset-Detail, „Other" unklickbar.
- Line/Area: echte Zeitachse (ISO-Daten rein, Formatierung an Achse/Tooltip via
  `$lib/format.ts`), Legende automatisch ab 2 Serien.
- **`colorForSymbol(symbol, allSymbols)`** in `$lib/charts/palette.ts`: Slot nach
  alphabetischer Position (nie nach Wert-Rang), `Other` und alles jenseits von
  Slot 7 fix grau.

### R3.2 — Serienpalette (Referenz)

**Entschieden (final): der Preset-Standard — die monochrome Teal-Skala**, Slots
1–5 = Preset, 6/7 = Teal-200/900, identisch in Light und Dark, `--chart-other`
= neutrales Stone-Grau. **Bewusster Trade-off:** Nachbarn unterscheiden sich nur
über Helligkeit — Identität tragen Legende, Tooltips und Direktlabels (Pflicht);
die formale CVD-Validierung wurde dafür ausgesetzt.

| Slot | Teal-Stufe | Wert (beide Modi) |
|---|---|---|
| 1 | 300 | `oklch(0.855 0.138 181.071)` |
| 2 | 500 | `oklch(0.704 0.14 182.503)` |
| 3 | 600 | `oklch(0.6 0.118 184.704)` |
| 4 | 700 | `oklch(0.511 0.096 186.391)` |
| 5 | 800 | `oklch(0.437 0.078 188.216)` |
| 6 | 200 | `oklch(0.91 0.096 180.426)` |
| 7 | 900 | `oklch(0.386 0.063 188.416)` |

- Grün/Rot der **Delta-Farben** für Gewinn/Verlust ([R11](04-seiten-und-flows.md#r11))
  sind ein eigener Satz (`#006300`/`#0ca30c` bzw. `#d03b3b`) — Delta immer mit
  Pfeil-Icon und Vorzeichen, nie Farbe allein.

**Offen (→ R11):** Der Zusammensetzungs-Line-Chart trimmt nicht — ab dem 8.
Asset werden Serien grau; erst der Top-7-Trim dort macht die Farben zwischen
Pie und Line vollständig konsistent.

<a id="r4"></a>
## R4 — Motion ✅

- Pie ~300 ms, Line/Area ~400 ms Einblendung (Tween).
- `prefers-reduced-motion`: Charts schalten auf `none` (reaktiv via Sveltes
  `prefersReducedMotion`), globaler CSS-Block in `app.css` drosselt alle
  Animationen/Transitions.
- **Offen (→ R11):** „nur beim ersten Render animieren" wird erst mit der
  Load-Konsolidierung möglich (heute remounten die `{#await}`-Blöcke pro
  Zeitraum-Wechsel); Zahlen-Tween für die Hero-Zahl kommt mit dem Hero.

## Definition of Done (Phase 1)

- [x] Theme-Toggle + `.dark` aktiv, ohne FOUC
- [x] Brand-Akzent über `--primary` (Teal-Preset)
- [x] Chart.js → LayerChart; Palette als Tokens, stabile Farbe pro Asset (F6)
- [x] Motion-Regeln inkl. `prefers-reduced-motion`
