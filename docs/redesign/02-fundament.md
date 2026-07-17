# Komponenten-Fundament — ✅ umgesetzt (Phase 2, 2026-07-17)

Kurzprotokoll. Alle Phase-2-Registry-Komponenten sind installiert (inkl.
`table`, `alert-dialog`, `dialog`, `toggle-group` für Phase 4); shadcn bleibt
Copy-in — Komponenten unverändert lassen, Anpassungen über Tokens.

<a id="r5"></a>
## R5 — Form-Primitives ✅

- Die drei Add-Formulare (`assets/add`, `integrations/add`,
  `integrations/[slug]/add`) und die **Login-Seite** neu auf `Input`/`Label`/
  `Select`/`Button`: `<label for>` an jedem Feld, Loading-Spinner am Submit,
  sichtbare Validierungstexte, keine hartkodierte Klassenwurst mehr.
- **R5.1 Combobox:** `$lib/components/search-combobox.svelte`
  (`command`+`popover`), max. 50 gerenderte Treffer. **Auswahl per
  `externalId`** — CoinGecko-Symbole sind nicht eindeutig, doppelte Keys brachen
  die Liste; nebenbei behoben: beim Speichern wurde vorher das *erste* Asset mit
  passendem Symbol genommen. Dieselbe Komponente für die Asset-Wahl bei
  Messungen.
- **R5.2** Doppel-Fetch behoben: ein `$effect` mit Race-Guard statt
  `onMount`+`$effect`. „ETF" aus der Auswahl entfernt (war nie speicherbar).
- **Rest:** `auth/register` hat noch die alten Klassen (war nicht in der
  Dreierliste) — beim nächsten Anfassen umstellen.

<a id="r6"></a>
## R6 — Feedback-Layer ✅

- `<Toaster />` (sonner) im Root-Layout, Theme folgt dem Theme-Store.
- **`mutate<T>()`** in `$lib/api/mutate.ts`: Erfolgs-Toast, Fehler-Toast mit
  Status→Text-Mapping (zeigt String-Bodies vom Backend direkt), `null` bei
  Fehler. Alle Mutationen laufen darüber — kein stilles Fehlschlagen mehr.
- Redirects via `goto()`; einzige `window.location`-Stelle ist der
  OIDC-Login (echter Full-Page-Redirect, gewollt).
- **Rest:** Die Lade-`{#await}`-Blöcke (Dashboard, Detailseiten) rendern noch
  rohe `error.message` — sie verschwinden mit den Phase-4-Umbauten (R11/R13).

<a id="r7"></a>
## R7 — API- & Auth-Layer ✅ (löst F3)

- Zentraler 401-Interceptor über `defaults.fetch` (`$lib/api/client.ts`),
  **gekeyt am angefragten Endpoint** (`/api/Auth/*` ausgenommen) — nicht an der
  aktuellen URL: bei SPA-Navigation laufen Loads (und ihre 401er) *vor* dem
  URL-Wechsel, ein Location-Check verschluckt sie (so gefunden und behoben).
- `returnUrl` beim Redirect; Login wertet ihn aus. „Session expired"-Toast nur,
  wenn vorher jemand eingeloggt war.
- `afterNavigate`-Check entfernt; `refreshUser()` (getMe + Config) wird von
  Layout-Start und Login geteilt. Logout räumt den User-Store.

<a id="r8"></a>
## R8 — Formatierung, Sprache & Svelte 5 — grösstenteils ✅

- **`$lib/format.ts`**: `formatCurrency`/`formatAmount`/`formatPercent`/
  `formatDate`, eine `LOCALE`-Konstante. **Entschieden: fix `de-CH`** —
  `navigator.language` wurde ausprobiert und verworfen (unkonfigurierte Browser
  melden `en-US` → `mm/dd/yyyy`-Daten). Falls je eine Nutzer-Einstellung kommt:
  Ein-Zeilen-Änderung an dieser Stelle.
- Charts (Achsen/Tooltips), Dashboard-Wert und Asset-Beträge laufen über
  `format.ts`. **Rest:** Report-`toFixed` fällt mit dem E8-Umbau (Phase 4).
- **Sprach-Sweep (E6):** läuft seitenweise beim Anfassen — Formulare, Login,
  Navigation, Seiten-Köpfe sind englisch; Rest mit Phase 4. Prüfliste:
  `grep -rniE '(ä|ö|ü|ß)' --include=*.svelte --include=*.ts src/`
- **Svelte-5-Vereinheitlichung & Datenladen:** beim Anfassen (nav-item,
  AssetMeasuringTiles etc. sind migriert); die verbleibenden `export let`-
  Komponenten und die Load-Vereinheitlichung gehören zu den Phase-4-Umbauten.

<a id="r9"></a>
## R9 — Hygiene ✅ (löst F4, bis auf Tests)

- Icon-Duplikat entfernt, `adapter-static` in devDeps, `generateGUID`/
  `helpers.ts` ersatzlos gestrichen.
- Zusätzlich erledigt: tote `tailwind.config.ts` gelöscht; Prettier-Plugins
  (`prettier-plugin-svelte` 4, `prettier-plugin-tailwindcss` 0.8) repariert —
  vorher crashte Prettier auf allen `.svelte`-Dateien; ESLint kann jetzt auch
  `*.svelte.ts` parsen.
- **Offen:** kein Frontend-Test. Erster Vitest-Happen: `colorForSymbol`,
  `formatCurrency`, `TrimMeasurings` (reine Funktionen).

## Definition of Done (Phase 2)

- [x] Registry-Komponenten installiert; keine handgebaute Form-Klassenwurst mehr
- [x] Coin-Auswahl ist eine durchsuchbare Combobox
- [x] Jede Mutation zeigt Erfolg **und** Fehler; kein `window.location.href` (ausser OIDC)
- [x] Jedes Feld hat `<label for>`; jeder Submit einen Loading-State
- [x] 401 → Login **mit** `returnUrl`; `/auth/me` nicht mehr pro Navigation
- [x] Beträge mit Tausendertrennung aus `$lib/format.ts`
- [ ] `border-gray`/`blue-500`-Grep sauber — Reste: `auth/register`, Teile von
  `assets/[slug]` (fallen mit Phase 4)
