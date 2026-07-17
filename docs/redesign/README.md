# Frontend-Redesign CryptoTracker

Stand: 2026-07-17 — **Phasen 1–3 sind umgesetzt** (Kurzprotokolle in den
Dokumenten), offen ist **Phase 4** ([04-seiten-und-flows.md](04-seiten-und-flows.md)).
Ziel: das Frontend visuell modernisieren **und** die User-Interaktionen
vereinfachen.

Dieses Verzeichnis ist getrennt von [`docs/refactoring/`](../refactoring/):
dort die *Bestandsaufnahme*, hier das *Vorhaben*. Die F-Punkte aus
[`06-frontend.md`](../refactoring/06-frontend.md) werden hier miterledigt und
dort abgehakt (F3, F4 und F6-Charts sind durch; F2/F5 bis auf Phase-4-Reste).

## Dokumente

| Datei | Inhalt | Status |
|---|---|---|
| [01-design-system.md](01-design-system.md) | Dark-Mode, Teal/Stone-Theme, LayerChart, Palette, Motion | ✅ Protokoll + Referenz |
| [02-fundament.md](02-fundament.md) | Form-Primitives, Combobox, Toasts, `mutate()`, Auth-Layer, `format.ts` | ✅ Protokoll |
| [03-navigation-und-layout.md](03-navigation-und-layout.md) | Navbar, Mobile-Sheet, Profil-Dropdown, Breadcrumb, `PageHeader` | ✅ Protokoll |
| [04-seiten-und-flows.md](04-seiten-und-flows.md) | Dashboard, Messungen (Inline-Edit), Assets/Integrationen, Report | **offen — der Plan** |

## Getroffene Entscheidungen

| # | Entscheidung | Stand |
|---|---|---|
| **E1** | **Expressiv, mit Charakter** — eigener Brand-Akzent, Gradient-Hero, grosse Typo, animierte Charts | Fundament ✅; Hero kommt mit R11 |
| **E2** | **Top-Navbar modernisieren** — Mobile-Menu, aktiver State, Profil-Dropdown, Theme-Toggle | ✅ |
| **E3** | **Interaktions-Fokus:** Dashboard/Charts · Formulare · Feedback/Toasts · Messungen direkt updaten | Formulare/Feedback ✅; Dashboard + Messungen = Phase 4 |
| **E4** | **Fundament zuerst, dann Seiten** | hat sich ausgezahlt — Phase-4-Seiten bauen nur noch zusammen |
| **E5** | **Standards statt Eigenbau** — shadcn-svelte konsequent, Chart.js → LayerChart | ✅ |
| **E6** | **UI konsequent Englisch, kein i18n** | Sweep läuft seitenweise; Rest mit Phase 4. Format-Locale bleibt fix `de-CH` ([02/R8](02-fundament.md#r8)) |
| **E7** | ~~Brand-Hue Violett~~ → **revidiert 2026-07-17: Teal/Stone via shadcn-Preset `b5dx81JH0`**; Serienpalette = Teal-Skala des Presets ([01/R3.2](01-design-system.md#r3)) | ✅ |
| **E8** | **Report bleibt eigene Seite** — als Stichtags-Bericht aufwerten | Phase 4 ([R13](04-seiten-und-flows.md#r13)) |
| **E9** | **`/info` löschen** — Seite + Nav-Eintrag | ✅ |
| **E10** | **Sync-Datum auf Integrations-Kacheln** — `IntegrationDto.LastSyncedAtUtc`, einziger Backend-Punkt | Phase 4 ([R13](04-seiten-und-flows.md#r13)) |

## Ausgangslage (historisch, komprimiert)

Die fünf Baustellen des Ist-Zustands vom 2026-07-16 — alle bis auf Reste erledigt:

1. Dark-Design existierte, war nie erreichbar → ✅ Phase 1
2. Formulare roh, 8× kopierte Klassenwurst an den Tokens vorbei → ✅ Phase 2
   (Rest: `auth/register`)
3. Mutationen ohne Fehler-Feedback, `window.location`-Reloads → ✅ Phase 2
4. Charts ohne Palette, Farbe nach Rang, Memory-Leak → ✅ Phase 1
5. Datenladen dreifach uneinheitlich, Svelte-4/5-Mix → teilweise; Rest fällt
   mit den Phase-4-Umbauten

**Der wichtigste Fund bleibt aktiv:** „Messwert einfach updaten" braucht keine
Backend-Änderung — `MeasuringService.AddIntegrationMeasuringAsync` ist ein
**Upsert** auf `(IntegrationId, Symbol, Date)`; es fehlt nur die UI
([R12](04-seiten-und-flows.md#r12)).

## Roadmap

- ✅ **Phase 1 — Design-System** (R1–R4): Dark-Mode + Toggle, Teal/Stone-Preset,
  LayerChart-Migration mit `colorForSymbol`, Motion inkl. Reduced-Motion
- ✅ **Phase 2 — Komponenten-Fundament** (R5–R9): Primitives + Combobox, Toasts +
  `mutate()`, 401-Interceptor mit `returnUrl`, `format.ts`, Hygiene
- ✅ **Phase 3 — Navigation & Layout** (R10): Navbar-Zielbild, Breadcrumb,
  `PageHeader` (inkl. Detailseiten, aus R13 vorgezogen)
- **Phase 4 — Seiten & Flows** (≈ 2–3 PT, offen):
  - **R11** Dashboard: Gradient-Hero mit Delta, ein globaler Zeitraum-Switch in
    der URL, ein Load statt drei `{#await}`, Empty-State
  - **R12** Messungen: Tabelle + Inline-Edit (Upsert!), Lösch-Dialog, Add als
    Dialog — drei Seiten werden eine
  - **R13** Assets-Kacheln mit Wert/Bestand, Integrations-Kacheln mit Badge +
    Sync-Datum (**E10**, einziger Backend-Punkt), Report-Umbau (**E8**)

## Offene Fragen

Keine — alle ursprünglich offenen Punkte sind entschieden (E1–E10); E7 wurde am
2026-07-17 auf Teal revidiert.
