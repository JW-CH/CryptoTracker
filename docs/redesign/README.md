# Frontend-Redesign CryptoTracker

Stand: 2026-07-16 (Basis Commit `c943176`). Ziel: das Frontend visuell
modernisieren **und** die User-Interaktionen vereinfachen.

Dieses Verzeichnis ist bewusst getrennt von [`docs/refactoring/`](../refactoring/):
dort steht die *Bestandsaufnahme* (Korrektheit, Sicherheit, Datenmodell), hier ein
*Vorhaben* mit Zielbild. Die noch offenen Punkte aus
[`06-frontend.md`](../refactoring/06-frontend.md) (F2 Formatierung, F3 Auth/Fehler,
F4 Hygiene, F5 Sprache, F6 Chart-Farben) werden hier **aufgegriffen statt
dupliziert** — jeder wird unten seiner Phase zugeordnet. Wenn ein F-Punkt hier
erledigt wird, gehört der Haken in `06-frontend.md`.

## Dokumente

| Datei | Inhalt |
|---|---|
| [01-design-system.md](01-design-system.md) | Tokens, Brand-Akzent, Typografie, Dark-Mode, validierte Chart-Palette, Motion |
| [02-fundament.md](02-fundament.md) | Form-Primitives, Toasts/Feedback, API- und Fehler-Layer, Formatierung, Svelte-5-Vereinheitlichung |
| [03-navigation-und-layout.md](03-navigation-und-layout.md) | Navbar, Mobile-Menu, Profil-Dropdown, Theme-Toggle, Seitengerüst |
| [04-seiten-und-flows.md](04-seiten-und-flows.md) | Dashboard, Assets, Integrationen, Messungen (Inline-Edit), Report |

## Getroffene Entscheidungen

Diese vier Entscheidungen sind der Rahmen; alles Weitere folgt daraus.

| # | Entscheidung | Konsequenz |
|---|---|---|
| **E1** | **Expressiv, mit Charakter** — eigener Brand-Akzent, Gradient-Hero, grosse Typo, animierte Charts | Eigene Farb-Identität statt shadcn-Default-Grau ([01](01-design-system.md)) |
| **E2** | **Top-Navbar modernisieren** — keine Sidebar; Mobile-Menu, aktiver State, Profil-Dropdown, Theme-Toggle | Kleinster struktureller Eingriff, Breadcrumb bleibt ([03](03-navigation-und-layout.md)) |
| **E3** | **Interaktions-Fokus:** Dashboard/Charts · Formulare · Feedback/Toasts · manuelle Messungen direkt updaten | Kein „Erfassen ohne Seitenwechsel" auf breiter Front — gezielt dort, wo es weh tut ([04](04-seiten-und-flows.md)) |
| **E4** | **Fundament zuerst, dann Seiten** | Phase 1–2 erzeugen wenig sichtbaren Fortschritt, machen aber jede spätere Seite billig ([02](02-fundament.md)) |
| **E5** | **Standards statt Eigenbau** — shadcn-svelte konsequent nutzen, Chart.js → LayerChart | Siehe unten; senkt den Aufwand, statt ihn zu erhöhen |
| **E6** | **UI konsequent Englisch, kein i18n** | Löst F5. Die ~38 UI-Strings (heute de/en gemischt) werden beim Anfassen vereinheitlicht, nicht in eine Übersetzungsschicht gehoben ([Details](#e6-im-detail--sprache)) |
| **E7** | **Brand-Hue Violett; Slot 7 fällt aus der Chart-Palette** | Serienpalette wird 7-slotting, neu geordnet und revalidiert ([R3.2](01-design-system.md#r3)); `--primary` = `#4a3aa7`/`#9085e9` ([R2](01-design-system.md#r2)) |
| **E8** | **Report bleibt eigene Seite** — als Stichtags-Bericht aufwerten | Kein Datums-Switch am Dashboard; Report bekommt in Phase 4 das volle Redesign ([R13](04-seiten-und-flows.md#r13)) |
| **E9** | **`/info` wird gelöscht** — Seite + Nav-Eintrag | R14 wird zur Lösch-Aufgabe; ein Nav-Slot weniger |
| **E10** | **Sync-Datum auf Integrations-Kacheln** — kleine Backend-Erweiterung genehmigt | `IntegrationDto.LastSyncedAtUtc` (max. `RecordedAtUtc`), einziger Backend-Punkt des Plans ([R13](04-seiten-und-flows.md#r13)) |

### E5 im Detail — „was die meisten verwenden"

Zwei Bibliotheks-Fragen, beide am Ist-Zustand geprüft statt nach Geschmack entschieden:

**UI-Komponenten → shadcn-svelte, und zwar das bereits vorhandene.** Der Befund
ist eindeutig: `components.json` existiert, `bits-ui` + `tailwind-variants` +
`cn()` sind installiert, `ui/button/button.svelte` ist eine unveränderte
shadcn-svelte-Komponente. Das Projekt steht also **schon** auf dem De-facto-Standard
für Svelte + Tailwind — es nutzt ihn nur kaum: **4 von ~50** Komponenten sind
installiert (`breadcrumb`, `button`, `card`, `skeleton`), der Rest wurde daneben
handgebaut. Die Formular-Baustelle ist damit kein Bauprojekt, sondern
`npx shadcn-svelte add …` ([02](02-fundament.md)).

<a id="e6-im-detail--sprache"></a>
### E6 im Detail — Sprache

**Die UI wird durchgängig Englisch. Kein i18n, keine Übersetzungsschicht.** Damit
ist F5 aus [`06-frontend.md`](../refactoring/06-frontend.md) entschieden — und
zwar auf die dort empfohlene Variante.

**Zur Klarstellung:** Das betrifft **nur die UI**. Diese Dokumentation bleibt
Deutsch, wie der Rest von `docs/`.

Warum kein i18n, obwohl es hier billig wäre (≈ 1–1.5 PT während des Redesigns,
weil die App eine reine Client-SPA ist — `ssr = false`, `prerender = false`,
also entfällt die ganze teure Server-Hälfte): Der einmalige Preis ist nicht das
Problem, sondern der **laufende Aufschlag von ~10–20 % auf jede künftige
UI-Arbeit** — jeder neue Text kostet dann zwei Einträge plus einen Key-Namen.
Für ein Publikum, das ohnehin Englisch liest, ist das Aufwand ohne Ertrag.
Eine Sprache, die alle verstehen, ist die billigere und bessere Lösung.

**Ist-Zustand (Basis für die Arbeit):** ~38 übersetzbare Strings im Markup, teils
**innerhalb derselben Seite** gemischt — „Speichern" neben `Confirm Password`,
„Vermögenswerte" neben `Login here`, dazu `home` und `info page`. Zwei Befunde:

- **Backend-i18n ist kein Thema.** Der F5-Teilbefund „teils deutsch aus dem
  Backend, `IntegrationController.cs:51`" ist **veraltet** — die 15
  Exception-Texte sind heute durchgängig Englisch („Asset not found",
  „Integration is not manual"). Sie laufen ohnehin über die Status→Text-Mapping-
  Schicht aus [R6](02-fundament.md#r6).
- `Fiat`, `Crypto`, `Stock`, `ETF`, `Commodity` sind **API-Enum-Werte**, keine
  UI-Texte — nicht übersetzen, höchstens für die Anzeige mappen.
- **Die einzige nicht-englische Stelle ausserhalb des Markups:**
  `assets/[slug]/+layout.ts:9` wirft `error(…, 'Asset konnte nicht geladen werden')`.
- `de-CH` ist in zwei `toLocaleDateString`-Aufrufen hartkodiert
  (`+page.svelte:25`, `assets/[slug]/+page.svelte:35`) — geht in `format.ts`
  auf ([R8](02-fundament.md#r8)). **Achtung, keine Automatik:** Sprache und
  Zahlenformat sind unabhängig. Englische UI heisst *nicht* automatisch
  `en-US`-Formate — Basiswährung und Datumsformat sind eine eigene
  Entscheidung (siehe R8).

**Charts → LayerChart 2.0.1 statt Chart.js.** Nicht aus Mode, sondern weil
Chart.js mit [E1](#getroffene-entscheidungen) und [R1](01-design-system.md#r1)
kollidiert: Canvas kann keine CSS-Variablen lesen, also müsste jeder Theme-Toggle
jeden Chart in JS neu einfärben und neu aufbauen. LayerChart rendert SVG — Tokens
wirken direkt, der Toggle kostet nichts. Dazu Svelte-5-nativ (Reaktivität und
Cleanup gratis, beides heute kaputt) und die Chart-Basis von shadcn-svelte, also
dieselbe Sprache wie der Rest. Geprüft und verworfen: **Unovis** (Peer-Deps nur
Svelte 3/4), **svelte-echarts** (verlangt `echarts ^5`, aktuell 6.1.0).
Begründung und Rückweg: [R3.1](01-design-system.md#r3).

## Executive Summary — der Ist-Zustand

**Gut und erhaltenswert:**

- **shadcn-svelte ist bereits sauber aufgesetzt** (`components.json`, `cn()`,
  `bits-ui`, `tailwind-variants`) — nur eben kaum genutzt. Der halbe Plan besteht
  darin, das einzulösen, was schon eingerichtet ist.
- Solide Basis: Tailwind 4 mit vollständigem Token-Set, generierter API-Client.
- Skeleton-Ladezustände sind flächendeckend da — das ist mehr, als die meisten
  Hobby-Projekte haben.
- `assets/+page.ts` zeigt bereits das *richtige* Muster (Load-Funktion + Streaming).
- Die Kachel-Grids für Assets/Integrationen sind hübsch und dürfen bleiben.

**Die fünf grössten Baustellen:**

1. **Das Dark-Design existiert, ist aber nie erreichbar** 🟠 — `app.css:42-74`
   definiert eine vollständige `.dark`-Palette, aber **nichts setzt je die
   `.dark`-Klasse**. Toter Code, den man geschenkt bekommt ([R1](01-design-system.md#r1)).
2. **Formulare sind roh** 🟠 — nackte `<select>`/`<input>` mit einer **8-fach
   kopierten** Tailwind-Klassenwurst (`assets/add` 3×, `integrations/[slug]/add` 3×,
   `integrations/add` 2×), hartkodiert auf `border-gray-200`/`blue-500` **an den
   Tokens vorbei** — die brechen im Dark-Mode sofort. Bemerkenswert:
   `assets/[slug]/edit` macht es **bereits richtig** (Token-Klassen, Labels,
   Loading-State) — die hausinterne Referenz existiert ([R5](02-fundament.md#r5)).
3. **Mutationen geben kein Fehler-Feedback** 🔴 — kein einziger Speichern-Flow
   zeigt einen Fehler an: die drei Add-Formulare haben zu
   `if (request.status == 200)` **keinen else-Zweig**, die Edit-Seite kein
   `catch`. Schlägt Speichern fehl, passiert sichtbar *nichts*. Die Add-Flows
   machen bei Erfolg zudem `window.location.href = …` = Full-Page-Reload
   ([R6](02-fundament.md#r6)).
4. **Charts setzen keinerlei Palette** 🟠 — `PieChart`/`LineChart` laufen auf
   Chart.js-Defaults; die `--chart-1..5`-Tokens sind **unbenutzt**. Farbe wird per
   Index vergeben, und weil `TrimMeasurings` nach Wert sortiert, **wechselt ein
   Asset die Farbe, wenn sich sein Rang ändert** — und hat in Pie und Line
   ohnehin verschiedene Farben. Dazu: nicht reaktiv, nie zerstört (Memory-Leak)
   ([R3](01-design-system.md#r3)).
5. **Datenladen ist dreifach uneinheitlich** 🟡 — `+page.ts` (Assets), `onMount`
   (Integrationen, Report), `{#await}` im Markup (Dashboard); dazu Svelte-4-`export let`
   neben Svelte-5-`$props` ([R8](02-fundament.md#r8)).

**Der angenehmste Fund:** „Messwert einfach updaten können" braucht **keine
Backend-Änderung**. `MeasuringService.AddIntegrationMeasuringAsync` ist bereits ein
**Upsert** auf `(IntegrationId, Symbol, Date)` — derselbe Endpoint nochmal
aufgerufen überschreibt den Wert. Es fehlt nur die UI dafür
([R12](04-seiten-und-flows.md#r12)).

## Roadmap

Phasen nach E4 geschnitten: Fundament zuerst. Jede Phase ist für sich mergebar.
Aufwand grob in Personentagen (PT).

### Phase 1 — Design-System (≈ 1.5–2 PT)
Das Fundament, auf dem alles Sichtbare aufsetzt.
- **R1** Theme-Toggle + `.dark` tatsächlich aktivieren, ohne FOUC
- **R2** Brand-Akzent über `--primary`, Typo-Skala, Gradient-Hero-Rezept (E1)
- **R3** Chart.js → LayerChart (E5); Palette als Tokens + **stabile Farbe pro Asset** (löst F6)
- **R4** Motion-Regeln (Chart-Animation, Hover, `prefers-reduced-motion`)

### Phase 2 — Komponenten-Fundament (≈ 1–2 PT)
*Günstiger als gedacht — die Primitives kommen aus der Registry (E5), nicht aus der Tastatur.*
- **R5** Form-Primitives holen: `input`, `label`, `select`, **Combobox** (`command`+`popover`, Coin-Liste!)
- **R6** Feedback-Layer: Toaster, Fehleranzeige, Submit-States (E3)
- **R7** API-Wrapper mit zentraler 401-Behandlung (löst F3)
- **R8** Formatierungs-Helper `formatCurrency`/`formatAmount` (löst F2) + Svelte-5-Vereinheitlichung
- **R9** Hygiene: doppelte Icon-Lib raus, `adapter-static` → devDeps (löst F4)

### Phase 3 — Navigation & Layout (≈ 0.5–1 PT)
- **R10** Navbar: Mobile-Menu, aktiver State, Profil-Dropdown, Theme-Toggle (E2)

### Phase 4 — Seiten & Flows (≈ 2–3 PT)
- **R11** Dashboard: Hero-Wert mit Delta, globaler Zeitraum-Switch, Tooltips, Empty-States
- **R12** Messungen: Inline-Edit des Werts (Upsert!), Tabelle statt Rohtext
- **R13** Assets/Integrationen/Report: Detailseiten, Formulare auf Primitives
- **R14** `/info` + Nav-Eintrag löschen (E9)

**Gesamt ≈ 5–8 PT.** Sichtbarer Durchbruch kommt am Ende von Phase 3.

## Offene Fragen

Keine — die ursprünglich offenen Punkte (Brand-Hue, Report, `/info`, Sync-Datum)
sind am 2026-07-16 als **E7–E10** entschieden, siehe Tabelle oben.
