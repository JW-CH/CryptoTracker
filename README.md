# CryptoTracker

A self-hosted cryptocurrency portfolio tracker that aggregates holdings from multiple exchanges and wallets, providing real-time price tracking and historical portfolio analysis.

## Features

- **Multi-Exchange Support**: Connect to Coinbase, Bitpanda, Crypto.com, and Binance
- **Wallet Tracking**: Track Bitcoin (XPUB/ZPUB), Ethereum, and XRP addresses directly
- **Portfolio History**: Daily snapshots with historical value tracking
- **Price Tracking**: Automatic price updates via CoinGecko
- **Authentication**: Username/password and OIDC (OpenID Connect) support
- **Manual Entries**: Add custom integrations and manual balance entries

## Quick Start

### Docker

```bash
docker run -d \
  -p 5106:8080 \
  -v ./config:/app/config \
  janmer/cryptotracker_web:latest
```

### Docker Compose (Recommended)

```yaml
version: '3.8'
services:
  cryptotracker:
    image: janmer/cryptotracker_web:latest
    ports:
      - "5106:8080"
    volumes:
      - ./config:/app/config
    depends_on:
      - postgres

  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: cryptotracker
      POSTGRES_USER: cryptotracker
      POSTGRES_PASSWORD: your-secure-password
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

## Configuration

Create a `config/config.yml` file:

```yaml
# Database connection (PostgreSQL required)
connectionstring: Host=postgres;Port=5432;Database=cryptotracker;Username=cryptotracker;Password=your-password

# Update interval in minutes (how often to sync balances)
interval: 120

# Log level: debug, info, warning, error
loglevel: info

# Timezone that defines the portfolio day (daily snapshot boundary), IANA id
timezone: Europe/Zurich

# Optional: stock price provider (enables price lookups for assets of type Stock)
stockapi: yahoofinance

# JWT Authentication
auth:
  secret: your-jwt-secret-minimum-32-characters
  issuer: https://your-domain.com
  audience: cryptotracker

# Optional: OIDC Provider (e.g., Keycloak, Authentik, PocketID)
oidc:
  clientid: your-client-id
  clientsecret: your-client-secret
  authority: https://your-oidc-provider.com

# Exchange integrations. Each integration is one account/wallet (names must be
# unique) and can have several sources, e.g. one hardware wallet holding coins
# on multiple chains. Balances of all sources are combined per day.
integrations:
  - name: My Coinbase
    description: Main Coinbase account
    sources:
      - type: coinbase
        key: your-api-key
        secret: your-api-secret

  - name: My Ledger
    description: Hardware wallet
    sources:
      - type: bitcoin
        key: zpub6abc...  # Your XPUB or ZPUB
      - type: ethereum
        key: 0xabc...     # Your address
```

> **Note:** Run only a single instance of the container. The background import
> and the automatic database migrations on startup are not coordinated across
> replicas — multiple instances would import duplicate data.

### Environment Variables

Every config value can be set (or overridden) via environment variables with the
`CRYPTOTRACKER_` prefix. Nested keys use a double underscore:

```bash
CRYPTOTRACKER_CONNECTIONSTRING="Host=postgres;..."   # overrides connectionstring
CRYPTOTRACKER_AUTH__SECRET="your-jwt-secret"          # overrides auth.secret
CONFIG_PATH=/etc/cryptotracker                        # config directory (default: ./config)
```

Environment variables take precedence over the config file; the file is optional
if everything is provided via environment.

### Supported Integration Types

| Type | Description | Key | Secret |
|------|-------------|-----|--------|
| `coinbase` | Coinbase Exchange | API Key | API Secret |
| `bitpanda` | Bitpanda | API Key | API Secret |
| `cryptocom` | Crypto.com Exchange | API Key | API Secret |
| `binance` | Binance | API Key | API Secret |
| `bitcoin` | Bitcoin Wallet | XPUB/ZPUB | (unused) |
| `ethereum` | Ethereum Address | Wallet Address | (unused) |
| `ripple` | XRP Address | Wallet Address | (unused) |

## Development Setup

### Prerequisites

- .NET 10.0 SDK
- Node.js 24+
- PostgreSQL 14+

### Backend

```bash
# Run the API (starts on localhost:5106)
make web

# Run tests
make test-webapi
make test-core
```

### Frontend

```bash
cd cryptotracker.web

# Install dependencies
npm install

# Start dev server (localhost:5173, proxies to API)
npm run dev

# Type checking
npm run check

# Linting
npm run lint
```

### Database Migrations

```bash
# Create a new migration
make ef_add_migration MigrationName

# Apply migrations
make ef_update_database
```

### API Client Generation

After modifying API endpoints, regenerate the TypeScript client:

```bash
# Ensure API is running first
make api
```

### Docker Build

```bash
# Development image
make docker_build_web_dev

# Production image
make docker_build_web
```

## Tech Stack

- **Backend**: ASP.NET Core 10.0, Entity Framework Core, PostgreSQL
- **Frontend**: SvelteKit, Svelte 5, Tailwind CSS 4, TypeScript
- **Authentication**: JWT, OpenID Connect
- **Exchange APIs**: CryptoClients.Net
- **Containerization**: Docker with multi-stage builds

## Roadmap

- [ ] Cardano wallet tracking
- [ ] Polkadot wallet tracking
- [ ] Ethereum token support
- [ ] XRP token support
- [ ] UI toast notifications
- [ ] Value obfuscation (show only percentages)

## License

This project is open source. See the repository for license details.
