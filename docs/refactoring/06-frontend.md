# Frontend (SvelteKit / Svelte 5)

Das Frontend ist insgesamt in gutem Zustand (Svelte 5, Tailwind 4, generierter
API-Client, shadcn-artige UI-Komponenten). Die Punkte hier sind kleiner als im
Backend, aber einige betreffen Korrektheit.

## F1 — Reaktivitätsfehler im Dashboard 🟠

`src/routes/+page.svelte`:

- ~~Set-Mutation im `{#each}`-Template~~ ✅ behoben 2026-07-10
  ([Bug 10](01-kritische-bugs.md)): Symbolliste wird pur aus den Response-Daten
  berechnet (`UniqueSymbols`).
- `summarize` ist als Toggle gedacht, aber weder `$state` noch je
  verändert — toter Schalter.
- `TrimMeasurings` wird pro Chart zweimal aufgerufen (Labels + Values, Zeile 70–71)
  und **sortiert das Array in place** (`data.sort`) — einmal berechnen, Ergebnis
  destrukturieren.
- Die drei Cards laden unabhängig via `{#await}` — `getMeasuringsByDays` und
  `getStandingsByDay` holen dieselben Daten doppelt (Standing = Summe der
  Measurings, siehe Backend `CryptoTrackerController.GetStandingByDay`). Ein
  gemeinsamer Load in `+page.ts` würde einen Request und Backend-Arbeit sparen.

## F2 — Datums-/Währungsformatierung dezentral 🟡

- `CHF` ist als String in mehreren Komponenten hartkodiert (`+page.svelte:52,85`,
  `report/+page.svelte:51`) — muss der konfigurierbaren Basiswährung folgen
  ([03/D4](03-datenmodell-und-aggregation.md#basiswaehrung)). Der Wert sollte vom
  Backend kommen (z. B. `/api/config` oder im `MeResponse`).
- `toFixed(2)`/`toFixed(8)` statt `Intl.NumberFormat('de-CH', { style: 'currency' })`
  — keine Tausendertrennung, hässliche Beträge bei großen Portfolios.
  → zentraler `formatCurrency`/`formatAmount`-Helper in `$lib`.
- `report/+page.svelte:18` schickt `date.toISOString()` an `GetMeasuringsByDate` —
  das Backend leitet daraus inzwischen sauber den Portfolio-Tag ab
  ([Bug 6](01-kritische-bugs.md) ist behoben), aber der Endpoint sollte trotzdem
  auf `DateOnly`-Strings (`yyyy-MM-dd`) umgestellt werden statt `DateTime` zu raten.

## F3 — Auth-Handling 🟡

- `+layout.svelte` ruft `checkAuth()` bei `onMount` **und** `afterNavigate` —
  jede Client-Navigation feuert ein `/api/auth/me`. Für eine SPA reicht: einmal
  beim Start + Reaktion auf 401-Antworten des API-Clients (oazapfts erlaubt
  `defaults`/Interceptor-artige Wrapper).
- Bei abgelaufenem JWT (60 min) bricht die Seite mitten in der Nutzung auf die
  Login-Seite ab, ohne Rückkehr zur vorherigen URL (`goto('/auth/login')` ohne
  `returnUrl`). Mit S4 (Refresh) zusammen lösen.
- Fehlerzustände der `{#await}`-Blöcke rendern rohe `error.message` — bei 401
  steht dort kryptisches Zeug. Einheitliche Fehlerkomponente + zentrale
  401-Behandlung.

## F4 — Abhängigkeiten & Projekt-Hygiene 🟡

- **Doppelte Icon-Library:** `lucide-svelte` **und** `@lucide/svelte` sind beide
  installiert (`package.json:19,34`) — eine entfernen (die `@lucide/svelte`-Variante
  ist die aktuelle).
- `@sveltejs/adapter-static` steht als einzige Runtime-`dependency` — gehört zu
  `devDependencies` (Build-Tool).
- `generateGUID()` in `$lib/helpers.ts` nutzt `Math.random` — wenn überhaupt
  nötig, `crypto.randomUUID()` verwenden; prüfen, ob die Funktion noch Verwendung hat.
- Kein einziger Frontend-Test (kein vitest/playwright konfiguriert). Minimum:
  `svelte-check` + `lint` in CI ([07](07-testing-und-devops.md)); mittelfristig
  Komponententests für die Chart-Datenaufbereitung (`TrimMeasurings` & Co. als
  reine Funktionen extrahieren — dann trivial testbar).

## F5 — Sprache der UI 🟡

UI-Texte sind gemischt deutsch („Aktueller Wert", „Keine Daten vorhanden") und
englisch (Navigation, Fehlermeldungen aus der API — teils deutsch aus dem
Backend, `IntegrationController.cs:51`). Entscheidung treffen: entweder
konsequent Deutsch (dann auch Fehlertexte über eine Mapping-Schicht) oder
i18n-Layer (paraglide/inlang ist bei SvelteKit üblich). Für ein
Self-Hosted-Projekt mit GitHub-Publikum: **englische Default-UI** empfohlen.

## F6 — Kleinigkeiten

- `report/+page.svelte` filtert `isHidden` client-seitig (Zeile 44), obwohl der
  Endpoint versteckte Assets bereits ausschließt — doppelte Logik, eine Quelle wählen.
- `data: api.AssetHoldingDto[] | null` ohne Initialisierung (`undefined` ≠ `null`,
  TS-Strictness prüfen); mit SvelteKit-Load-Funktionen (`+page.ts`) statt
  `onMount`-Fetches bekäme man Typen, SSR-Fähigkeit und Ladezustände geschenkt —
  aktuell wird durchgängig das `onMount`/`{#await}`-Muster verwendet, was mit
  `adapter-static` funktioniert, aber Navigations-Flackern erzeugt.
- Chart-Farben/Optionen je Komponente dupliziert (`PieChart`/`LineChart` je
  eigene Palette) — Design-Token-Datei wäre konsistenter.
