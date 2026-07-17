# Navigation & Layout — ✅ umgesetzt (Phase 3, 2026-07-17)

Kurzprotokoll.

<a id="r10"></a>
## R10 — Navbar ✅

- **Links:** Wortmarke „CryptoTracker" mit Brand-Icon; **Mitte:** vier Links
  (Home, Report, Integrations, Assets — Info ist weg, E9) mit aktivem State
  (Unterstrich in `--primary`; exakter Vergleich für `/`, `startsWith` sonst);
  **Rechts:** Theme-Toggle + **Profil-Dropdown** (Avatar-Initialen, Name/E-Mail,
  Logout) — ersetzt das tote „Profil"-Item und den separaten Logout-Link.
- **Mobile:** unter `md` Burger-`sheet` von links; Logo, Toggle und Avatar
  bleiben sichtbar; Link-Klick schliesst das Sheet.
- `border-b` + Blur, sticky; `class`-Prop und Svelte-4-Syntax entfernt;
  Labels englisch.
- Fixes im selben Zug: Logout setzt `user`-Store zurück (vorher zeigte die
  Navbar nach dem Logout weiter den Avatar — war vom entfernten
  `afterNavigate`-Check übertüncht).

## Seitengerüst ✅

- Layout-Container `py-8 space-y-6` — Inhalt klebt nicht mehr an der Navbar.
- **Breadcrumb reaktiv** (`$derived` aus `page.url`) — das `{#key}`-Remount im
  Layout ist weg (Verdacht bestätigt); erscheint erst **ab Tiefe 2**, letzte
  Stufe als `Breadcrumb.Page` statt Link.
- **`<PageHeader>`** (`$lib/components/page-header.svelte`) extrahiert —
  erweitert um Slots `media` (Avatar/Bild), `meta` (Badge), `subtitle`,
  `actions`. Verwendet auf den Listen-Seiten **und** (aus R13 vorgezogen) auf
  beiden Detailseiten; dabei ersetzt `Badge` die handkodierte Amber/Blau-Pille
  auf `integrations/[slug]`.
- Footer (auskommentiert + tote Komponente) gelöscht.

## Definition of Done (Phase 3)

- [x] Navigation auf dem Handy bedienbar (Sheet)
- [x] Aktiver Nav-State; `/` markiert nicht überall mit
- [x] Theme-Toggle in der Navbar (Light/Dark/System)
- [x] Kein totes „Profil"-Item — Dropdown mit Logout
- [x] Inhalt klebt nicht an der Navbar; `PageHeader` extrahiert
- [x] Footer gelöscht
