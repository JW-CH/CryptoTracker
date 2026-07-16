# Komponenten-Fundament

Phase 2 nach [E4 „Fundament zuerst"](README.md#getroffene-entscheidungen).
Wenig sichtbarer Fortschritt, aber danach ist jede Seite in Phase 4 billig.

## Die Ausgangslage ist besser als sie aussieht

**shadcn-svelte ist bereits vollständig eingerichtet.** `components.json` existiert
(Registry `shadcn-svelte.com/registry`, `baseColor: slate`, TypeScript, Aliases auf
`$lib`), `src/lib/utils.ts` enthält `cn()` und die `WithElementRef`-Typen, und
`bits-ui@2.18.1` + `tailwind-variants@3.2.2` sind installiert.
`ui/button/button.svelte` ist eine **unveränderte** shadcn-svelte-Komponente.

Installiert sind aber nur **vier** Komponenten: `breadcrumb`, `button`, `card`,
`skeleton`. Alles andere — Formulare, Feedback — wurde **von Hand daneben gebaut**,
statt es zu holen.

> **Das ist die zentrale Erkenntnis für Phase 2.** Die Frage „welche Komponenten
> nehmen wir?" ist längst beantwortet: shadcn-svelte ist der De-facto-Standard für
> Svelte + Tailwind (dieselbe Familie wie shadcn/ui für React), das Projekt steht
> schon drauf, und die Registry deckt fast alles ab, was der Plan braucht. Der
> Grossteil von R5 und R6 ist deshalb **kein Implementierungsaufwand, sondern ein
> CLI-Aufruf**. Konsequenz für die Schätzung: Phase 2 fällt von „bauen" auf
> „holen + verdrahten".

```bash
cd cryptotracker.web
npx shadcn-svelte@latest add input label select field command popover dialog \
  alert-dialog sonner dropdown-menu badge table alert separator tooltip \
  sheet avatar toggle-group chart
```

(Alle 19 Namen gegen den Registry-Index geprüft — inklusive `chart`: die
Registry liefert auch die Chart-Wrapper auf LayerChart-Basis mit, siehe
[R3.1](01-design-system.md#r3). Die Liste deckt jeden Verweis in diesem Plan:
`alert-dialog` für R12, `sheet`/`avatar` für R10, `toggle-group` für R11.)

Ein Punkt zur Erwartungshaltung: shadcn ist **Copy-in**, kein Paket. Die Dateien
landen in `src/lib/components/ui/` und gehören dann euch — inklusive Pflege. Das
ist gewollt (deshalb passen sie sich an eure Tokens an), heisst aber: Updates
kommen nicht per `npm update`. Genau deshalb sollten sie **unverändert** bleiben
und Anpassungen über Tokens laufen ([R2](01-design-system.md#r2)).

<a id="r5"></a>
## R5 — Form-Primitives 🟠

**Befund:** Formulare sind der mit Abstand schwächste Teil des Frontends.
Dieselbe Tailwind-Klassenwurst ist **acht Mal** kopiert:

```
rounded-lg border-2 border-solid border-gray-200 px-3 py-2 pe-9 text-sm
focus:border-blue-500 focus:ring-blue-500 disabled:pointer-events-none
disabled:opacity-50 dark:border-neutral-700 dark:bg-neutral-900 ...
```

— in `assets/add/+page.svelte` (3×), `integrations/[slug]/add/+page.svelte` (3×),
`integrations/add/+page.svelte` (2×).

**Gegenbeispiel im eigenen Haus:** `assets/[slug]/edit/+page.svelte` (aus dem
jüngsten Commit) macht fast alles richtig — Token-Klassen (`border-input`,
`focus:ring-ring`), echte `<label for>`, `saving`-State mit `disabled`,
`goto()` + `invalidateAll()` statt Full-Reload. **Diese Seite ist der Massstab**,
an dem die drei Add-Formulare ausgerichtet werden; ihr `inputClass`-String wandert
dabei in die `Input`-Komponente. Einzige Lücke: kein `catch` — ein Fehler beim
Speichern verpufft als unbehandelte Rejection ([R6](#r6)).

Was daran konkret falsch ist:

- **Am Token-System vorbei:** `border-gray-200`, `blue-500`, `neutral-900` sind
  hartkodiert. Es gibt `--input`, `--ring`, `--border` — sie werden ignoriert.
  Der Brand-Akzent aus [R2](01-design-system.md#r2) wird an diesen Feldern
  **wirkungslos** sein.
- **Die `dark:`-Klassen hier sind das einzige Dark-Handling der App** — und sie
  raten Farben, die nicht zur `.dark`-Palette passen. Mit [R1](01-design-system.md#r1)
  wird das sichtbar kaputt.
- **Keine Labels.** `AssetType:` und `asset:` sind **roher Text neben dem Feld**,
  kein `<label for>`. Screenreader finden nichts, Klick aufs Wort fokussiert nicht.
- **Keine Validierung, kein Fehlertext.** `assets/add` macht `if (!symbol) return;`
  und `if (!externalId) return;` — der Klick auf „Speichern" tut dann einfach
  **gar nichts**. Kein Hinweis, keine Markierung.
- **Kein Disabled/Loading am Submit** → Doppelklick feuert zwei Requests.

**Zielbild:** `Input`, `Label`, `Select`, `Field` aus der Registry; alle
hartkodierten Farben raus. Jedes Feld bekommt ein `<Label for>`, jeder Submit
einen `disabled`+Spinner-State.

### R5.1 — Combobox für die Coin-Liste 🟠

**Der schlimmste Einzelfall.** `assets/add/+page.svelte:92-101` rendert die
**gesamte CoinGecko-Liste** in ein natives `<select>` — das sind **Tausende**
`<option>`-Elemente. Nicht durchsuchbar (ausser per Tipp-Präfix), unbrauchbar auf
Mobile, und ein spürbarer DOM-Klotz.

→ `command` + `popover` = **Combobox mit Suche** (genau der Standard-shadcn-Weg).
Bei der Grösse zusätzlich: Ergebnisse auf ~50 Treffer begrenzen, Suche
debounced. Dieselbe Combobox löst auch die Asset-Auswahl in
`integrations/[slug]/add`.

### R5.2 — Nebenbefund: `$effect` als Fetch-Trigger

`assets/add/+page.svelte:14-20` hat **beides**: `onMount(async () => values = await GetStuff(assetType))`
**und** ein `$effect`, das bei `assetType`-Änderung dasselbe tut. Das `$effect`
feuert auch initial → **doppelter Fetch beim ersten Laden**. Dazu fehlt eine
Race-Absicherung: schnelles Umschalten Fiat→Crypto→Fiat kann die Antworten in
falscher Reihenfolge einsetzen. `onMount` streichen, `$effect` mit
Abbruch-Guard behalten — oder sauberer in eine Load-Funktion ([R8](#r8)).

<a id="r6"></a>
## R6 — Feedback-Layer 🔴

**Befund — das ist der gravierendste UX-Fehler der App.** Die drei Add-Flows
folgen diesem Muster:

```js
let request = await api.addAsset({ ... });
if (request.status == 200) {
    window.location.href = '/assets/' + symbol;
}
```

- **Es gibt keinen `else`-Zweig.** Schlägt der Request fehl — 400, 409 „Asset
  existiert bereits", 500, Netzwerk weg — passiert **sichtbar nichts**. Der Nutzer
  klickt „Speichern", der Knopf federt zurück, und er bleibt ratlos auf dem
  Formular. Dasselbe in `integrations/[slug]/add` und `integrations/add`.
- **Kein `try/catch`:** oazapfts wirft bei Netzwerkfehlern → unbehandelte
  Promise-Rejection. Auch die sonst vorbildliche Edit-Seite hat nur
  `try/finally` ohne `catch` — der Fehler verpufft dort genauso stumm.
- **`window.location.href` ist ein Full-Page-Reload** — verwirft den kompletten
  SPA-State, lädt Bundle + Auth-Check neu, erzeugt einen Weiss-Blitz. In einer
  SvelteKit-App ist das `goto()`. `deleteMeasuring` in
  `integrations/[slug]/measurings` prüft immerhin `if (x.data)`, aber auch dort:
  kein Fehlerpfad.
- **Erfolg wird nie bestätigt.** Nach dem Redirect ist unklar, ob es geklappt hat.

**Zielbild:**

1. **`sonner`** (Toast, in der Registry) — `<Toaster />` einmal in `+layout.svelte`.
2. Ein Helper, der das Muster kapselt, damit es nicht wieder 8× kopiert wird:
   ```ts
   // $lib/api/mutate.ts
   export async function mutate<T>(
     fn: () => Promise<{ status: number; data: T }>,
     opts: { success: string; onSuccess?: (data: T) => void }
   ): Promise<T | null>
   ```
   → fängt Fehler, zeigt Erfolgs-Toast, mappt Status-Codes auf lesbare Meldungen,
   gibt bei Fehler `null` zurück.
3. **Redirects über `goto()`**, nie `window.location`.
4. **Fehlertexte:** Aktuell rendern die `{#await}`-Catch-Blöcke rohe
   `error.message` (F3) — bei einem 401 steht dort Kryptisches. Eine
   `<ErrorState />`-Komponente + Status→Text-Mapping. Zusammen mit dem
   Backend-Punkt „Fehler-Middleware mit ProblemDetails"
   ([05-backend-codequalitaet](../refactoring/05-backend-codequalitaet.md))
   angehen — dann liefert das Backend die Texte gleich mit.

<a id="r7"></a>
## R7 — API- & Auth-Layer 🟡 (löst F3)

**Befund:** `+layout.svelte` ruft `checkAuth()` in `onMount` **und** in
`afterNavigate` — **jede** Client-Navigation feuert ein `/api/auth/me`. Für eine
SPA reicht: einmal beim Start + Reaktion auf 401.

**Zielbild:**
- Zentraler oazapfts-Wrapper mit 401-Interceptor → `user.set(null)` + Redirect.
- **`returnUrl` mitgeben.** Heute wirft ein abgelaufenes JWT (60 min) den Nutzer
  mitten in der Arbeit auf `/auth/login` — **ohne Rückweg** zur Seite, auf der er
  war. Nach dem Login landet er stumpf auf Home.
- `afterNavigate`-Check entfernen.

Details unter F3 in [06-frontend.md](../refactoring/06-frontend.md); mit dem
Refresh-Token-Thema (S4) zusammen denken.

<a id="r8"></a>
## R8 — Formatierung, Sprache & Svelte-5-Vereinheitlichung 🟡 (löst F2 + F5)

**Sprach-Sweep (E6): UI konsequent Englisch, kein i18n.** ~38 Strings, mechanische
Arbeit — aber sie muss *jemand machen*, sonst bleibt die Entscheidung Theorie.
Kein eigener Arbeitsschritt: **beim Anfassen der jeweiligen Seite miterledigen**
(Phase 3–4). Ein separater Durchgang wäre ein zweites Mal dieselben Dateien öffnen.

- Heute mischt sich beides *innerhalb* einer Seite: „Speichern" neben
  `Confirm Password`, „Vermögenswerte" neben `Login here`, dazu `home`,
  `info page`, `More`.
- **Nicht vergessen** — die einzige Stelle ausserhalb des Markups:
  `assets/[slug]/+layout.ts:9` → `error(…, 'Asset konnte nicht geladen werden')`.
- Auch `<title>cryptotracker</title>` (`+layout.svelte:48`) ist der einzige
  Seitentitel der ganzen App — jede Seite zeigt denselben. Beim Sweep pro Route
  setzen.
- **Nicht** übersetzen: `Fiat`/`Crypto`/`Stock`/`ETF` sind API-Enum-Werte.
- Prüfliste am Ende: `grep -rniE '(ä|ö|ü|ß)' --include=*.svelte --include=*.ts src/`
  liefert nur noch Fundstellen in Kommentaren.

**Formatierung:** `toFixed(2)`/`toFixed(8)` überall — **keine Tausendertrennung**.
Ein Portfolio von 42318.55 liest sich als `42318.55`. Bei Beträgen ab fünf Stellen
ist das schlicht schlecht lesbar, und E1 stellt die Zahl gross in den Hero.

→ `$lib/format.ts`:
```ts
const LOCALE = 'de-CH';          // ← eine Konstante, eine Entscheidung, ein Ort

formatCurrency(value, currency)  // Intl.NumberFormat(LOCALE, { style: 'currency' })
formatAmount(value, symbol)      // adaptive Nachkommastellen: 8 für BTC, 2 für Fiat
formatPercent(value)             // mit Vorzeichen, für Deltas
formatDate(date)                 // ersetzt das inline toLocaleDateString im Dashboard
```

**Sprache ≠ Zahlenformat.** Englische UI ([E6](README.md#e6-im-detail--sprache))
heisst *nicht* automatisch `en-US`. Bei CHF als Basiswährung ist Schweizer
Formatierung (`42'318.55`, `16.07.2026`) sachlich richtig und `en-US`
(`42,318.55`, `7/16/2026`) schlicht falsch — der Apostroph als Tausendertrenner
ist hier Konvention. **Empfehlung: `de-CH` als Format-Locale behalten**, auch bei
englischer UI. Das ist kein Widerspruch, sondern der Normalfall. Als **eine
Konstante** in `format.ts`, nicht 20× verstreut — dann ist es eine
Ein-Zeilen-Änderung, falls es je anders sein soll.
`toFixed(8)` für **jedes** Asset (Report) ist ohnehin falsch — `1'250.00000000 EUR`
ist Unsinn. Nachkommastellen gehören an den `assetType`.

**Svelte-5-Vereinheitlichung:** Das Projekt mischt zwei Epochen:
- **Svelte 4:** `export let` in `AssetTiles`, `IntegrationTiles`,
  `AssetMeasuringTiles`, `PieChart`, `LineChart`, `navbar`
- **Svelte 5:** `$props`/`$state` in `+page.svelte`, `assets/+page.svelte`

Beim Anfassen jeweils auf `$props()` ziehen. Kein Selbstzweck: `export let` +
`$state` in derselben Codebase ist eine Stolperfalle bei der Reaktivität — genau
die Klasse Bug, die F1/Bug 10 schon einmal produziert hat.

**Datenladen vereinheitlichen:** drei Muster nebeneinander —
`+page.ts`-Load (Assets, **das gute**), `onMount`-Fetch (Integrationen, Report,
Integration-Details), `{#await}` im Markup (Dashboard). Ziel: durchgängig
`+page.ts` mit Streaming, wie es `assets/+page.ts` vormacht. Das ist
`adapter-static`-kompatibel und beseitigt das Navigations-Flackern (F6).

<a id="r9"></a>
## R9 — Hygiene 🟡 (löst F4)

Kleinkram, der beim Anfassen mitgeht:

- **Doppelte Icon-Library:** `lucide-svelte` **und** `@lucide/svelte` sind beide
  in `package.json`. `@lucide/svelte` ist die aktuelle → die andere raus. Beide
  parallel bedeutet zwei Icon-Sets im Bundle.
- **`@sveltejs/adapter-static` ist die einzige `dependency`** — ein Build-Tool
  gehört in `devDependencies`.
- **`generateGUID()`** (`$lib/helpers.ts`) nutzt `Math.random`. Wird nur von den
  Charts für DOM-IDs gebraucht — mit [R3](01-design-system.md#r3) (`bind:this`
  statt `getElementById`) fällt die Funktion **ersatzlos weg**. Sonst
  `crypto.randomUUID()`.
- **Kein Frontend-Test.** Minimum: `svelte-check` + `lint` in CI. Die reinen
  Funktionen aus diesem Plan (`colorForSymbol`, `formatCurrency`, `TrimMeasurings`)
  sind der ideale erste Vitest-Happen — genau das schlägt F4 vor.

## Definition of Done (Phase 2)

- [ ] Registry-Komponenten installiert; **keine handgebaute Form-Klassenwurst mehr**
- [ ] Kein `border-gray-200`/`blue-500`/`neutral-900` mehr im Code (`grep`!)
- [ ] Coin-Auswahl ist eine durchsuchbare Combobox, kein 1000-Options-`<select>`
- [ ] **Jede** Mutation zeigt Erfolg **und** Fehler an; kein `window.location.href` mehr
- [ ] Jedes Feld hat ein `<label for>`; jeder Submit einen Loading-State
- [ ] 401 führt zu Login **mit** `returnUrl`; `/auth/me` feuert nicht mehr pro Navigation
- [ ] Beträge mit Tausendertrennung aus `$lib/format.ts`
