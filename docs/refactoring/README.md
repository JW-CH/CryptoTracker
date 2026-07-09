# Refactoring-Plan CryptoTracker

Stand: 2026-07-09. Basis: Commit `fb1c4d3` auf `main`.

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

**Die fünf größten Baustellen (Details in den verlinkten Dokumenten):**

1. **Fiat-Bewertung ist invertiert** — EUR/USD-Bestände werden mit dem falschen
   Wechselkurs bewertet (Faktor ~1.18 bzw. ~1.53 zu hoch). Bestätigt gegen die
   Frankfurter-API. → [01, Bug 1](01-kritische-bugs.md#bug-1)
2. **Verkaufte Assets zählen ewig weiter** — das Forward-Fill in `ApiHelper` trägt
   den letzten bekannten Bestand unbegrenzt fort; Exchanges melden aber nur
   Balances > 0. Wer alles verkauft, sieht den Bestand trotzdem für immer.
   → [01, Bug 2](01-kritische-bugs.md#bug-2) und [03](03-datenmodell-und-aggregation.md)
3. **Jeder kann sich registrieren und sieht das gesamte Portfolio** — es gibt
   keine Datenhoheit pro Benutzer und keinen Schalter, Registrierung zu deaktivieren.
   → [02](02-sicherheit.md)
4. **Die Aggregation lädt die komplette Messhistorie in den Speicher** — die
   Batch-Abfrage filtert nicht auf den angefragten Zeitraum; das skaliert mit der
   Tabellengröße, nicht mit den angefragten Tagen. → [03](03-datenmodell-und-aggregation.md)
5. **Integrations-Abrufe, die still fehlschlagen, löschen Tagesdaten** — der
   Import löscht erst die heutigen Messungen und schreibt dann die neuen; liefert
   die Exchange-API einen Fehler (kein Throw, nur leere Liste), bleibt der Tag leer.
   → [01, Bug 5](01-kritische-bugs.md#bug-5)

## Empfohlene Reihenfolge (Roadmap)

Die Phasen sind so geschnitten, dass jede für sich mergebar ist und die späteren
Phasen auf den früheren aufbauen. Grobe Aufwandsschätzung in Personentagen (PT).

### Phase 1 — Korrektheit (≈ 3–5 PT)
Falsche Zahlen sind für einen Portfolio-Tracker das schlimmste Problem.

- Fiat-Kursrichtung fixen (Bug 1)
- Forward-Fill begrenzen / Null-Bestände schreiben (Bug 2)
- Währungs-Casing vereinheitlichen (Bug 3)
- AssetType-Erkennung beim Import (Bug 4)
- Import-Fehlerbehandlung: keine Löschung ohne erfolgreichen Abruf (Bug 5)
- UTC/Lokalzeit-Konsistenz herstellen (Bug 6)

### Phase 2 — Sicherheit (≈ 2–3 PT)
- Registrierung per Config abschaltbar machen (Default: aus, wenn schon ein User existiert)
- Login-Härtung (Lockout, Rate-Limiting)
- Entscheidung dokumentieren: Single-Tenant (ein Portfolio, mehrere Logins) oder Multi-User

### Phase 3 — Datenmodell & Aggregation (≈ 5–8 PT)
- `AssetMeasuring` auf Tages-Snapshot mit natürlichem Schlüssel `(IntegrationId, Symbol, Date)` umstellen
- Aggregation auf datumsbereichs-beschränkte Abfragen umbauen, Indexe ergänzen
- `TimeProvider` einführen (testbare Zeit), Basiswährung konfigurierbar machen

### Phase 4 — Architektur (≈ 5–8 PT)
- Integration-Provider-Pattern statt `switch` (eine Klasse pro Exchange)
- Service-Schicht zwischen Controller und EF; DTOs raus aus dem database-Projekt
- Preis-Provider vereinheitlichen (`IPriceProvider` pro AssetType)
- ~~`cryptotracker.worker` löschen~~ (✅ erledigt 2026-07-09), tote Codepfade entfernen

### Phase 5 — Robustheit & Code-Qualität (≈ 3–5 PT)
- `IHttpClientFactory` überall, Retry/Rate-Limit (CoinGecko!), TTL-Caches
- Fehler-Middleware mit ProblemDetails statt generischer Exceptions
- CancellationTokens durchreichen, Naming-Bereinigung (`MessungDto`, `IntegrationShit`, …)

### Phase 6 — Frontend (≈ 2–4 PT)
- Svelte-5-Reaktivität fixen (`$state`, Seiteneffekte im Template)
- Zentrale Formatierungs-Helper, Fehlerbehandlung, Sprach-Entscheidung (de/en)

### Phase 7 — Testing & CI (≈ 3–5 PT)
- Aggregationslogik testen (der komplexeste Code hat null Tests)
- Testcontainers statt InMemory für DB-nahe Tests
- GitHub Actions: Build, Test, Lint, Docker-Push

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
