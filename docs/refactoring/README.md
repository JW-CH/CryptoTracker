# Refactoring-Plan CryptoTracker

Stand: 2026-07-09 (Bestandsaufnahme, Basis Commit `fb1c4d3`),
aktualisiert 2026-07-10. Erledigte Punkte werden aus den Dokumenten entfernt
bzw. als Kurzprotokoll geführt — die ursprünglichen Befunde stehen in der
Git-Historie.

Dieses Verzeichnis dokumentiert eine vollständige Bestandsaufnahme des Projekts mit
konkreten Befunden, hinterfragten Design-Entscheidungen und Verbesserungsvorschlägen.
Jedes Dokument ist so geschrieben, dass die Punkte später einzeln abgearbeitet werden können.

## Dokumente

| Datei | Inhalt |
|---|---|
| [01-kritische-bugs.md](01-kritische-bugs.md) | Konkrete Fehler mit falschen Zahlen/Datenverlust — zuerst fixen |
| [02-sicherheit.md](02-sicherheit.md) | Offene Registrierung, fehlende Datenhoheit, Secrets, Auth-Härtung |
| [03-datenmodell-und-aggregation.md](03-datenmodell-und-aggregation.md) | Das Kernstück: Snapshot-Modell, Forward-Fill, Zeitzonen, Währung — mit Zielbild |
| [04-architektur.md](04-architektur.md) | Projektstruktur, Schichten, Provider-Pattern für Integrationen, DI |
| [05-backend-codequalitaet.md](05-backend-codequalitaet.md) | HttpClient-Nutzung, Fehlerbehandlung, Naming, tote Codepfade |
| [06-frontend.md](06-frontend.md) | Svelte-5-Reaktivität, Formatierung, Duplikate, API-Client |
| [07-testing-und-devops.md](07-testing-und-devops.md) | Testlücken, InMemory-Falle, CI/CD, Deployment |

## Executive Summary — was ist gut, was nicht

**Gut und erhaltenswert:**

- Klare Grundidee des Datenmodells: Bestände (`AssetMeasuring`) und Preise
  (`AssetPriceHistory`) getrennt speichern, Wert erst bei der Abfrage berechnen.
  Das ist die richtige Entscheidung und sollte bleiben.
- Generierter API-Client (oazapfts) statt handgeschriebener Fetch-Aufrufe.
- Central Package Management, aktuelle Framework-Versionen (.NET 10, Svelte 5, Tailwind 4).
- Multi-Stage-Dockerfile mit Non-Root-User.
- JWT + optionales OIDC mit sinnvollen Defaults.

**Die fünf größten Baustellen der Bestandsaufnahme — alle behoben:**

1. ~~Fiat-Bewertung invertiert~~ ✅ 2026-07-09 (Altdaten 2026-07-11 mit dem
   Snapshot-Umbau endgültig verworfen)
2. ~~Verkaufte Assets zählen ewig weiter~~ ✅ 2026-07-09 (0-Messungen + `maxfilldays`)
3. ~~Offene Registrierung auf gemeinsamem Portfolio~~ ✅ 2026-07-09 entschärft
   (First-User-Setup; Grundsatzfrage Single-/Multi-Tenant weiter offen, [02](02-sicherheit.md))
4. ~~Aggregation lädt komplette Messhistorie~~ ✅ 2026-07-09/11
   (Holdings datumsbegrenzt + `AsNoTracking`; Preiszeilen-Fenster noch offen, [03/D3](03-datenmodell-und-aggregation.md))
5. ~~Fehlgeschlagene Abrufe löschen Tagesdaten~~ ✅ 2026-07-09 (seit dem
   Snapshot-Upsert wird strukturell nie gelöscht)

## Empfohlene Reihenfolge (Roadmap)

Die Phasen sind so geschnitten, dass jede für sich mergebar ist und die späteren
Phasen auf den früheren aufbauen. Grobe Aufwandsschätzung in Personentagen (PT).

### Phase 1 — Korrektheit ✅
- ~~Bugs 1–10~~ ✅ erledigt 2026-07-09/10/11 (Bug 6 via `PortfolioClock` +
  Snapshot-Modell, [01](01-kritische-bugs.md))

### Phase 2 — Sicherheit ✅ (bis auf Grundsatzfrage)
- ~~First-User-Setup, Lockout, Rate-Limiting~~ ✅ erledigt 2026-07-09/10
- **Offen: Entscheidung dokumentieren** — Single-Tenant oder Multi-User ([02/S1](02-sicherheit.md))

### Phase 3 — Datenmodell & Aggregation — WEITGEHEND ERLEDIGT
- ~~`DailyHolding`-Tages-Snapshot mit PK `(IntegrationId, Symbol, Date)`~~ ✅ 2026-07-11
  (Altdaten bewusst verworfen, [03/D2](03-datenmodell-und-aggregation.md))
- ~~`TimeProvider`/`PortfolioClock` (testbare Zeit, konfigurierbare Zeitzone)~~ ✅ 2026-07-11
- ~~Basiswährung konfigurierbar~~ ✅
- **Offen:** Preiszeilen-Fenster + Preis-Index ([03/D3](03-datenmodell-und-aggregation.md));
  größere Grundsatzfragen D1 (Asset-Surrogate-Key) und D5 (Integrationen in die DB)

### Phase 4 — Architektur ✅
- ~~Integration-Provider-Pattern, Service-Schicht, DTO-Umzug, Preis-Provider,
  Config via IConfiguration, tote Codepfade~~ ✅ erledigt 2026-07-09/10
  (Kurzprotokoll in [04](04-architektur.md); Rest: Exchange-SDK-DI)

### Phase 5 — Robustheit & Code-Qualität (≈ 2–3 PT) — TEILWEISE
- ~~`IHttpClientFactory` in eigenen HTTP-Aufrufen, TTL-Caches, Naming~~ ✅
- **Offen:** Retry/Backoff + Chunking (CoinGecko), Timeouts,
  Fehler-Middleware mit ProblemDetails, CancellationTokens ([05](05-backend-codequalitaet.md))

### Phase 6 — Frontend (≈ 2–4 PT) — TEILWEISE
- ~~Seiteneffekt im Template (Bug 10)~~ ✅ 2026-07-10
- **Offen:** zentrale Formatierungs-Helper, Auth-Handling/401, Sprach-Entscheidung (de/en) ([06](06-frontend.md))

### Phase 7 — Testing & CI (≈ 2–3 PT) — TEILWEISE
- ~~Aggregations-, Provider-, Service- und Import-Tests~~ ✅ (71 Tests, [07/T1](07-testing-und-devops.md))
- **Offen:** Auth-Tests, Testcontainers statt InMemory, GitHub Actions (Build, Test, Lint, Docker-Push)

## Leitfragen, die vor Phase 3/4 beantwortet werden sollten

Diese Entscheidungen prägen das Zielbild; sie sind in den Dokumenten jeweils
mit Empfehlung diskutiert:

1. **Ein Portfolio oder mehrere?** Aktuell faktisch Single-Tenant. Multi-User
   nachzurüsten ist teuer — bewusst entscheiden, nicht implizit lassen.
2. **Integrationen in Config oder DB?** Aktuell beides (YAML für API-Integrationen,
   DB für manuelle). Empfehlung: alles in die DB, Secrets verschlüsselt, UI-verwaltbar.
3. **Symbol als Identität?** `Asset.Symbol` ist Primärschlüssel. Kollisionen zwischen
   Crypto/Aktien/Fiat (z. B. „EUR" als Fiat und als Token) sind real. Empfehlung:
   surrogate Key + `(Symbol, AssetType)` unique.
4. **Wie viel Historie ist eine Messung?** Aktuell: mehrere Messungen pro Tag,
   die täglich gelöscht und neu geschrieben werden. Empfehlung: genau ein
   Snapshot pro (Integration, Asset, Tag), idempotent per Upsert.
