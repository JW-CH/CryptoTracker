# Sicherheit

Stand 2026-07-10: S1 ist entschärft, S2 vollständig erledigt (siehe
Kurzprotokoll unten). Offen sind die Grundsatzfrage aus S1 sowie S3–S7.

## S1 — Grundsatzfrage: Single- oder Multi-Tenant 🟠

Die akute Schwachstelle (offene Registrierung auf einem gemeinsamen Portfolio)
ist seit 2026-07-09 entschärft: First-User-Setup (Registrierung offen, solange
kein User existiert, danach zu; Override `auth.allowregistration`),
OIDC-Auto-Provisioning bewusst an (eigener IdP), per `oidc.autoprovision: false`
abschaltbar.

**Offen bleibt die Grundsatzentscheidung:** Ist CryptoTracker Single-Tenant
(ein Portfolio, mehrere gleichberechtigte Logins) oder Multi-User? `Asset`,
`ExchangeIntegration` und `AssetMeasuring` haben keinen Bezug zu einem Benutzer —
jeder Login sieht und verwaltet alles. Beides ist legitim, sollte aber eine
dokumentierte Entscheidung sein. Multi-User bedeutet `UserId` auf Integrationen
(transitiv auf Messungen), Filterung in jeder Query, Migration der Bestandsdaten;
Aufwand ≈ 3–5 PT, am besten zusammen mit dem Datenmodell-Umbau
([03](03-datenmodell-und-aggregation.md)).

## S3 — Exchange-Secrets im Klartext in `config.yml` 🟠

Alle API-Keys/Secrets liegen unverschlüsselt in einer YAML-Datei
(`CryptoTrackerIntegration`). Positiv: `config/config.yml` ist gitignored
(geprüft), und `example-config.yml` enthält nur Platzhalter.

Risiken: Backups, Docker-Volumes und `docker inspect`-fähige Bind-Mounts
enthalten die Keys; die Datei wird beim Start komplett eingelesen und die
Secrets leben in einem Singleton.

**Teilweise entschärft durch A5 (2026-07-10):** Secrets können jetzt per
Env-Var injiziert werden (`CRYPTOTRACKER_AUTH__SECRET`,
`CRYPTOTRACKER_INTEGRATIONS__0__SECRET`, …) — die Datei kann secrets-frei
bleiben, der Orchestrator liefert die Werte.

**Verbleibend:** Zusammen mit dem Umzug der Integrationen in die DB
([03/D5](03-datenmodell-und-aggregation.md)) die Secrets mit ASP.NET Data
Protection verschlüsseln (Key-Ring auf Volume). Außerdem: Exchange-Keys als
**read-only** anlegen im README dokumentieren.

## S4 — JWT-Details 🟡

- **Kein Refresh-Mechanismus:** Token und Cookie laufen nach `ExpiryMinutes`
  (Default 60) ab; der Benutzer wird kommentarlos ausgeloggt (Frontend leitet auf
  Login um). Entweder Expiry ehrlich verlängern oder Sliding-Refresh einbauen.
- **Issuer aus dem Request-Host** (`JwtService.GetIssuer`): ohne konfigurierten
  Issuer wird der Host des Login-Requests zum Issuer, validiert wird er dann
  nicht (`ValidateIssuer = false`). Funktioniert, aber der dynamische Issuer hat
  dann schlicht keinen Zweck — entweder validieren oder weglassen.
- **Kein `NameIdentifier`/`sub`-basiertes Lookup:** `Me()` löst den User über die
  E-Mail-Claim auf. Bei E-Mail-Änderung invalidiert das bestehende Tokens nicht,
  sondern lässt sie auf einen ggf. anderen Account zeigen. UserId als Claim
  verwenden.
- Cookie-Settings (`HttpOnly`, `SameSite=Strict`, `Secure` bei HTTPS) sind gut.
  `Secure` hängt aber am Scheme des Requests — hinter einem TLS-terminierenden
  Proxy ohne korrekte Forwarded-Headers wird das Cookie unsicher gesetzt. Siehe S5.

## S5 — ForwardedHeaders ohne KnownProxies 🟡

`Program.cs` akzeptiert `X-Forwarded-Host/-Proto` von **jedem** Client
(Default `KnownProxies`/`KnownNetworks` werden nicht gesetzt, aber
`UseForwardedHeaders` mit expliziten Options überschreibt die Defaults, die
sonst nur Loopback erlauben — prüfen!). Ein Client kann damit Scheme/Host
spoofen, was in den JWT-Issuer (`GetIssuer`) und das `Secure`-Flag des Cookies
einfließt. `KnownNetworks`/`KnownProxies` konfigurieren oder Middleware nur
aktivieren, wenn ein Proxy konfiguriert ist. Betrifft auch das
IP-basierte Rate-Limiting (S2): ohne `X-Forwarded-For` teilen sich alle
Clients hinter dem Proxy einen Bucket (strenger, nicht schwächer).

## S6 — Externe Dienste erhalten Wallet-Adressen und XPUBs 🟡

Für BTC/ETH/XRP werden Adressen bzw. **XPUB/ZPUB** (daraus sind *alle* Adressen
der Wallet ableitbar!) an blockchain.info, ethplorer.io (mit `freekey`) und
xrpscan.com gesendet (`Bitcoin`/`Ethereum`/`RippleIntegrationProvider`). Das ist
funktional notwendig, sollte aber im README als Privacy-Tradeoff dokumentiert
werden; optional eigene Node/anderer Provider konfigurierbar.

## S7 — Fehler-Antworten leaken Interna 🟡

Die Services werfen inzwischen typisiert (`KeyNotFoundException`,
`InvalidOperationException` mit englischen Meldungen), aber ohne
Exception-Middleware enden weiterhin alle als 500 (mit Stacktrace in Dev).
Einheitliche Fehler-Middleware mit ProblemDetails einführen; fachliche Fehler
als 4xx mappen. Details in [05/Q1](05-backend-codequalitaet.md#fehlerbehandlung).

## Erledigt (Kurzprotokoll)

- **S2 — Lockout & Rate-Limiting:** Lockout via `CheckPasswordSignInAsync(...,
  lockoutOnFailure: true)` (2026-07-09); Rate-Limiting via `AddRateLimiter`,
  Policy „auth" (Fixed Window, 10 Req/min pro Client-IP, 429) auf
  `login`/`register`, live verifiziert (2026-07-10).

## Positiv

- `config.yml` ist gitignored; keine Secrets in der Git-Historie gefunden
  (`git log -- config/config.yml` leer).
- JWT-Secret-Mindestlänge wird erzwungen.
- Swagger nur in Development.
- Docker-Container läuft als Non-Root (`APP_UID`).
- Auth-Endpoints (`login`/`register`) sind rate-limitiert.
