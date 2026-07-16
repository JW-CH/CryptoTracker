# Navigation & Layout

Phase 3, nach [E2 „Top-Navbar modernisieren"](README.md#getroffene-entscheidungen).
Keine Sidebar — die bestehende Struktur bleibt, wird aber erwachsen.

<a id="r10"></a>
## R10 — Navbar

**Befund** (`lib/components/navigation/navbar.svelte`, 26 Zeilen):

```svelte
<NavItem path="/" text="Home" />
...
{#if $user != null}
    <NavItem text="Profil ({$user.displayName ?? $user.email})" />
    <NavItem path="/auth/logout" text="Logout" />
```

- **Kein Mobile-Handling.** Sieben Nav-Items in einer `flex`-Zeile mit
  `space-x-4`. Auf einem Handy laufen die schlicht aus dem Bild oder brechen
  hässlich um — es gibt **keinen** Breakpoint, kein Burger-Menu. Die App ist auf
  Mobile faktisch nicht navigierbar.
- **`Profil (…)` hat kein `path`** — ein Nav-Item, das aussieht wie ein Link, aber
  **ins Leere klickt**. Toter Eintrag, der nur den Namen anzeigt.
- **Kein Theme-Toggle** — der Hebel, der [R1](01-design-system.md#r1) überhaupt
  erreichbar macht.
- **`class`-Prop wird durchgereicht, aber nie gesetzt** (`export { className as class }`
  → kein Aufrufer übergibt etwas). Toter Parameter.
- `border-b-2` ist ein ungewöhnlich dicker Rahmen; `border-b` + leichter Blur
  wirkt zeitgemässer.
- Svelte-4-Syntax (`export let`) → [R8](02-fundament.md#r8).

**Zielbild:**

```
┌────────────────────────────────────────────────────┐
│ ◆ CryptoTracker   Home Report Integr. Assets  ☾ JW │
│                        ▔▔▔▔▔▔                       │
└────────────────────────────────────────────────────┘
```

- **Links:** Logo/Wortmarke mit Brand-Akzent ([R2](01-design-system.md#r2)) — der
  einzige Ort neben dem Hero, wo Brand-Farbe Fläche bekommt.
- **Mitte:** Nav-Items mit **aktivem State**. `nav-item.svelte` prüft ihn heute
  nicht — via `page.url.pathname` markieren (Unterstrich in `--primary` oder
  `bg-accent`-Pill). Achtung: `/` matcht sonst jeden Pfad → exakter Vergleich für
  Home, `startsWith` für die übrigen.
- **Rechts:** Theme-Toggle (Sonne/Mond, `dropdown-menu` mit Light/Dark/System) +
  **Profil-Dropdown** (`dropdown-menu`): Avatar-Initialen → Name/E-Mail als
  Header, dann „Logout". Ersetzt das tote Profil-Item **und** den separaten
  Logout-Link — zwei Nav-Slots gespart.
- **Mobile:** unter `md` klappt die Navigation in ein `sheet`/Burger-Menu; Logo
  und Theme-Toggle bleiben sichtbar.
- **Info-Eintrag entfällt** (E9, [R14](04-seiten-und-flows.md#r13)) — zusammen
  mit dem Profil-Dropdown schrumpft die Nav von 7 auf 4 Links: Home, Report,
  Integrations, Assets.

Alles vorhanden in der Registry (`dropdown-menu`, `sheet`, `avatar`) — siehe
[Phase 2](02-fundament.md).

## Seitengerüst

**Befund** (`+layout.svelte:51-56`):

```svelte
<div class="container mx-auto px-6 pb-8">
    {#key page.url.pathname}
        <NavBreadcrumb />
    {/key}
```

- **Kein `pt-`** — der Seiteninhalt klebt direkt unter der Navbar.
- Das `{#key page.url.pathname}` erzwingt ein Remount des Breadcrumbs bei jeder
  Navigation. Wenn `NavBreadcrumb` seinen Pfad reaktiv aus `page.url` ableitet,
  ist das **überflüssig** — beim Anfassen prüfen.
- Der auskommentierte `<!-- <Footer /> -->` und `lib/components/footer.svelte`:
  entweder benutzen oder löschen. Toter Code.

**Zielbild:** `container mx-auto px-6 py-8 space-y-6`, Breadcrumb nur ab Tiefe ≥ 2
(auf `/` ist er sinnlos), darunter ein einheitlicher **Page-Header**
(`h1` + optionale Aktion rechts). Das Muster steht bereits identisch in
`assets/+page.svelte:17-20` und `integrations/+page.svelte:19-22` — als
`<PageHeader title actions>` extrahieren, dann verschwindet die Dopplung.

## Definition of Done (Phase 3)

- [ ] Navigation ist auf dem Handy bedienbar (Burger/Sheet)
- [ ] Aktiver Nav-State sichtbar; `/` markiert nicht überall mit
- [ ] Theme-Toggle in der Navbar, schaltet Light/Dark/System
- [ ] Kein totes „Profil"-Item mehr — Dropdown mit Logout
- [ ] Inhalt klebt nicht mehr an der Navbar; `PageHeader` extrahiert
- [ ] Footer benutzt oder gelöscht
