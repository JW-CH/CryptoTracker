# Sicherheit

## S1 — Offene Registrierung auf einem gemeinsamen Portfolio 🔴

**Befund:** `AuthController.Register` (`AuthController.cs:84`) ist anonym erreichbar.
Gleichzeitig gibt es **keine Datenhoheit**: `Asset`, `ExchangeIntegration` und
`AssetMeasuring` haben keinen Bezug zu einem Benutzer. Jeder, der den Server
erreicht, kann sich registrieren und sieht sofort das komplette Portfolio inkl.
aller Bestände und Integrationsnamen — und kann Assets löschen/verstecken und
manuelle Messungen anlegen.

Für einen Self-Hosted-Tracker hinter VPN mag das akzeptabel sein, aber das README
bewirbt öffentliches Docker-Deployment. Das ist die größte einzelne Schwachstelle.

**Empfehlung (gestaffelt):**

1. **Sofort:** Config-Flag `auth.allowRegistration` (Default: `false`, sobald
   mindestens ein Benutzer existiert — „First-User-Setup"-Muster). OIDC-Auto-
   Provisionierung (`Program.cs:180`) separat schaltbar machen
   (`oidc.autoProvision`), denn auch dort legt jedes gültige OIDC-Konto einen
   User an.
2. **Grundsatzentscheidung:** Ist CryptoTracker Single-Tenant (ein Portfolio,
   mehrere gleichberechtigte Logins) oder Multi-User? Beides ist legitim —
   aber es sollte eine dokumentierte Entscheidung sein. Multi-User bedeutet
   `UserId` auf Integrationen (und damit transitiv auf Messungen), Filterung in
   jeder Query, Migration der Bestandsdaten. Aufwand ≈ 3–5 PT, am besten
   zusammen mit dem Datenmodell-Umbau ([03](03-datenmodell-und-aggregation.md)).

## S2 — Kein Lockout / Rate-Limit beim Login 🟠

`AuthController.Login` benutzt `CheckPasswordAsync` direkt — das umgeht die
Lockout-Zählung von ASP.NET Identity komplett. Brute-Force ist unbegrenzt möglich.

**Fix:** `SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`
verwenden und Identity-Lockout-Optionen konfigurieren. Zusätzlich ASP.NET
Rate-Limiting-Middleware auf `/api/auth/*`.

## S3 — Exchange-Secrets im Klartext in `config.yml` 🟠

Alle API-Keys/Secrets liegen unverschlüsselt in einer YAML-Datei
(`CryptoTrackerIntegration`). Positiv: `config/config.yml` ist gitignored
(geprüft), und `example-config.yml` enthält nur Platzhalter.

Risiken: Backups, Docker-Volumes und `docker inspect`-fähige Bind-Mounts
enthalten die Keys; die Datei wird beim Start komplett eingelesen und die
Secrets leben in einem Singleton.

**Empfehlung:** Zusammen mit dem Umzug der Integrationen in die DB
([04 → A3](04-architektur.md#a3)) die Secrets mit ASP.NET Data Protection
verschlüsseln (Key-Ring auf Volume). Mindestens aber: Env-Var-Substitution in der
Config unterstützen (`secret: ${COINBASE_SECRET}`), damit Secrets über den
Orchestrator injiziert werden können. Wichtig: Exchange-Keys als **read-only**
anlegen dokumentieren (README erwähnt das nicht).

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

`Program.cs:217` akzeptiert `X-Forwarded-Host/-Proto` von **jedem** Client
(Default `KnownProxies`/`KnownNetworks` werden nicht gesetzt, aber
`UseForwardedHeaders` mit expliziten Options überschreibt die Defaults, die
sonst nur Loopback erlauben — prüfen!). Ein Client kann damit Scheme/Host
spoofen, was in den JWT-Issuer (`GetIssuer`) und das `Secure`-Flag des Cookies
einfließt. `KnownNetworks`/`KnownProxies` konfigurieren oder Middleware nur
aktivieren, wenn ein Proxy konfiguriert ist.

## S6 — Externe Dienste erhalten Wallet-Adressen und XPUBs 🟡

Für BTC/ETH/XRP werden Adressen bzw. **XPUB/ZPUB** (daraus sind *alle* Adressen
der Wallet ableitbar!) an blockchain.info, ethplorer.io (mit `freekey`) und
xrpscan.com gesendet (`CryptoTrackerLogic.cs:257,226,204`). Das ist funktional
notwendig, sollte aber im README als Privacy-Tradeoff dokumentiert werden;
optional eigene Node/anderer Provider konfigurierbar.

## S7 — Fehler-Antworten leaken Interna 🟡

Controller werfen generische `Exception`s („Asset not found", aber auch EF-Fehler),
die als 500 mit Stacktrace (Dev) bzw. nichtssagend (Prod) enden. Einheitliche
Fehler-Middleware mit ProblemDetails einführen; fachliche Fehler als 4xx.
Details in [05](05-backend-codequalitaet.md#fehlerbehandlung).

## Positiv

- `config.yml` ist gitignored; keine Secrets in der Git-Historie gefunden
  (`git log -- config/config.yml` leer).
- JWT-Secret-Mindestlänge wird erzwungen (`Program.cs:104`).
- Swagger nur in Development.
- Docker-Container läuft als Non-Root (`APP_UID`).
