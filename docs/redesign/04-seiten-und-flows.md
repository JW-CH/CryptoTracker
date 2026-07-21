# Seiten & Flows — ✅ umgesetzt (Phase 4, 2026-07-21)

Kurzprotokoll. Damit ist das Redesign (Phasen 1–4) komplett.

<a id="r11"></a>

## R11 — Dashboard (`routes/+page.svelte`) ✅

- **Ein Load** in `+page.ts`: einzelner `getMeasuringsByDays(range)`-Call statt
  drei `{#await}`; Standings werden aus den Measurings abgeleitet (kein
  separater Request mehr, F1).
- **Hero über die volle Breite**, Gradient-Fläche (`from-primary to-primary/75`),
  eingebettetes `LineChart` (Gradient-Fill, `smooth`, ohne Achsen) oben in der
  Karte, darunter Wert (`text-5xl md:text-6xl`), Delta mit Icon/Vorzeichen und
  der globale Zeitraum-`ToggleGroup` (7/30/90/365) — Zeitraum liegt in der URL
  (`?range=`).
- Allocation (Pie) + Composition (Line) darunter, beide mit derselben
  Top-7-+-„Other"-Trimmung (`TrimMeasurings`) für konsistente Farben.
- Empty-State mit Link zu „Integrations", wenn noch keine Daten vorhanden sind.
- Skeletons (`rounded-4xl`) passend zur Luma-Kartenform.

<a id="r12"></a>

## R12 — Manuelle Messungen: Wert direkt updaten ✅

- `integrations/[slug]/measurings/+page.svelte` als **eine** Ledger-Tabelle
  (`table`), absteigend nach Datum sortiert **im Script**.
- **Inline-Edit:** Stift-Icon → Input, Enter speichert per
  `addIntegrationMeasuring` (bestehender Upsert, kein neuer Endpoint), Escape
  bricht ab.
- **Löschen mit Bestätigung** (`alert-dialog`) statt scharfem X.
- **„+ Messung" als `Dialog`** statt eigener Route (Datum vorbelegt auf heute,
  Asset per Combobox) — mehrere Messungen am Stück ohne Full-Reload.
- Alte Route `integrations/[slug]/add/` gelöscht.

<a id="r13"></a>

## R13 — Restliche Seiten ✅

- **Assets:** Kacheln zeigen jetzt Wert + Bestand
  (`assets/+page.ts` lädt `getAssets` + `getLatestMeasurings` parallel);
  Empty-State ergänzt.
- **Integrationen:** Kacheln auf `Badge` (statt hartkodiertem Amber/Blau) +
  `IntegrationAvatar` umgestellt; zeigen „Last sync"/„Last measurement"
  relativ (`formatRelativeTime`). Dafür **einziger Backend-Punkt des Plans
  (E10):** `IntegrationDto.LastSyncedAtUtc` (Subquery auf `DailyHoldings`,
  `IntegrationService.cs`), TS-Client neu generiert.
- **Report:** volles Redesign statt Kosmetik (E8) — Datums-Navigation
  (Date-Input + Vor/Zurück, Datum in der URL), echte `Table` (Asset/Amount/
  Price/Value/Share%, `tabular-nums`), Summenzeile, Empty-State. Client-seitige
  `isHidden`-Filterung entfernt (Endpoint filtert bereits, F6).
- **Info:** gelöscht (E9, bereits Phase 3).

## Definition of Done (Phase 4)

- [x] Dashboard beantwortet „mehr oder weniger als gestern?" ohne Nachdenken
- [x] Ein globaler Zeitraum-Switch, in der URL
- [x] Kein leerer 3-Spalten-Raum mehr neben dem Hero
- [x] Messwert lässt sich inline korrigieren, ohne Löschen+Neuanlegen
- [x] Löschen fragt nach
- [x] Mehrere Messungen am Stück erfassbar, ohne Full-Reload
- [x] Report hat Datums-Navigation und Tabelle mit Summenzeile (E8)
- [x] Integrations-Kacheln zeigen den letzten Sync (E10)
- [x] `/info` + Nav-Eintrag gelöscht (E9)

## Bewusst zurückgestellt (kein Redesign-Scope)

- `assets/[slug]/+page.svelte`: alte native `<select>`-Elemente fürs
  Asset-Linking, deutsche Textreste, ein paar bekannte ESLint-Altlasten
  (`require-each-key`, `no-navigation-without-resolve`).
- `auth/register/+page.svelte`: noch auf alten hartkodierten Tailwind-Klassen.
- Vitest-Setup fehlt weiterhin (F4) — Kandidaten: `colorForSymbol`,
  `formatCurrency`, `TrimMeasurings`.
- Mehrere Seiten (`integrations/`, `integrations/[slug]`, `assets/[slug]`,
  `measurings`-Ledger) laufen noch über `onMount`/`{#await}` statt `+page.ts`
  (F6) — im Plan nie gefordert, bleibt allgemeine Hygiene-Backlog.
