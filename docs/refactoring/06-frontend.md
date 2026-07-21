# Frontend (SvelteKit / Svelte 5)

Das Frontend ist insgesamt in gutem Zustand (Svelte 5, Tailwind 4, generierter
API-Client, shadcn-artige UI-Komponenten). Die Punkte hier sind kleiner als im
Backend, aber einige betreffen Korrektheit.

## F1 — Reaktivitätsfehler im Dashboard 🟠

`src/routes/+page.svelte`:

- ~~Set-Mutation im `{#each}`-Template~~ ✅ behoben 2026-07-10
  ([Bug 10](01-kritische-bugs.md)): Symbolliste wird pur aus den Response-Daten
  berechnet (`UniqueSymbols`).
- ~~`summarize` ist als Toggle gedacht, aber weder `$state` noch je
  verändert — toter Schalter.~~ ✅ entfernt 2026-07-17 (Redesign Phase 1/R3)
- ~~`TrimMeasurings` wird pro Chart zweimal aufgerufen (Labels + Values)
  und **sortiert das Array in place**~~ ✅ behoben 2026-07-17: Kopie statt
  In-Place-Sort, ein Aufruf via `{@const}` (Redesign Phase 1/R3)
- ~~Die drei Cards laden unabhängig via `{#await}`~~ ✅ 2026-07-21: ein
  gemeinsamer Load in `+page.ts` (Redesign Phase 4/R11), Standing wird aus den
  Measurings abgeleitet statt separat geholt.

## F2 — Datums-/Währungsformatierung dezentral 🟡

- ~~`CHF` ist als String in mehreren Komponenten hartkodiert~~ ✅ läuft über den
  `baseCurrency`-Store aus `/api/config`.
- ~~`toFixed(2)`/`toFixed(8)` statt `Intl.NumberFormat` — keine
  Tausendertrennung~~ ✅ 2026-07-17: `$lib/format.ts`
  (`formatCurrency`/`formatAmount`/`formatPercent`/`formatDate`, Locale fix
  `de-CH`); Report-Reste folgen mit dem E8-Umbau (Redesign Phase 4)
- `report/+page.svelte:18` schickt `date.toISOString()` an `GetMeasuringsByDate` —
  das Backend leitet daraus inzwischen sauber den Portfolio-Tag ab
  ([Bug 6](01-kritische-bugs.md) ist behoben), aber der Endpoint sollte trotzdem
  auf `DateOnly`-Strings (`yyyy-MM-dd`) umgestellt werden statt `DateTime` zu raten.

## F3 — Auth-Handling 🟡

- ~~`checkAuth()` bei `onMount` **und** `afterNavigate`~~ ✅ 2026-07-17: Check
  nur noch beim Start; zentraler 401-Interceptor über `defaults.fetch`
  (`$lib/api/client.ts`), gekeyt am **Endpoint** statt an der aktuellen URL
  (Race bei SPA-Navigation), `/api/Auth/*` ausgenommen.
- ~~Abbruch auf Login ohne Rückkehr~~ ✅ 2026-07-17: `returnUrl` wird mitgegeben
  und vom Login ausgewertet; „Session expired"-Toast nur, wenn vorher jemand
  eingeloggt war. (S4/Refresh-Token bleibt ein eigenes Backend-Thema.)
- ~~Fehlerzustände der `{#await}`-Blöcke rendern rohe `error.message`~~
  ✅ 2026-07-21: Dashboard/Report/Assets laufen über `+page.ts`-Loads mit
  eigenem Empty-/Fehler-State statt rohem `error.message` (Redesign R11/R13).
  Verbleibende Seiten (`integrations/`, `integrations/[slug]`,
  `assets/[slug]`, Measurings-Ledger) sind noch auf `onMount`/`{#await}` — kein
  Redesign-Scope, siehe F6.

## F4 — Abhängigkeiten & Projekt-Hygiene 🟡

- ~~**Doppelte Icon-Library**~~ ✅ 2026-07-17: `lucide-svelte` entfernt,
  `@lucide/svelte` auf 1.x.
- ~~`@sveltejs/adapter-static` als Runtime-`dependency`~~ ✅ 2026-07-17: in
  `devDependencies`.
- ~~`generateGUID()`~~ ✅ 2026-07-17: mit der LayerChart-Migration ersatzlos
  gestrichen (`$lib/helpers.ts` gelöscht).
- Kein einziger Frontend-Test (kein vitest/playwright konfiguriert). Minimum:
  `svelte-check` + `lint` in CI ([07](07-testing-und-devops.md)); mittelfristig
  Komponententests für die Chart-Datenaufbereitung (`TrimMeasurings` & Co. als
  reine Funktionen extrahieren — dann trivial testbar).

## F5 — Sprache der UI 🟡

**Entschieden (Redesign E6, 2026-07-16): konsequent englische UI, kein i18n.**
Sweep läuft seitenweise beim Anfassen — Formulare, Login, Navigation, Assets-
und Integrations-Köpfe sind umgestellt; der Rest (Dashboard-Texte, Report,
Detailseiten-Bodies) folgt mit den Phase-4-Umbauten. Der Teilbefund „deutsch
aus dem Backend" war zuletzt schon veraltet (Exception-Texte sind englisch).

## F6 — Kleinigkeiten

- ~~`report/+page.svelte` filtert `isHidden` client-seitig~~ ✅ 2026-07-21:
  entfernt, Endpoint filtert bereits (Redesign R13).
- `data: api.AssetHoldingDto[] | null` ohne Initialisierung — für Dashboard,
  Report und Assets erledigt durch die Umstellung auf `+page.ts`-Loads
  (Redesign R11/R13: Typen, SSR-Fähigkeit, kein Navigations-Flackern mehr).
  Weiterhin offen für `integrations/`, `integrations/[slug]`, `assets/[slug]`
  und die Measurings-Ledger — dort noch `onMount`/`{#await}`.
- ~~Chart-Farben/Optionen je Komponente dupliziert~~ ✅ 2026-07-17: Farben
  kommen aus den `--chart-*`-Tokens via `colorForSymbol`
  (`$lib/charts/palette.ts`), Charts auf LayerChart migriert.
