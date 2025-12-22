# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

### Backend (.NET)
```bash
make web                    # Run webapi (localhost:5106)
make test-webapi            # Run API tests
make test-core              # Run core logic tests
dotnet build                # Build all projects
```

### Frontend (SvelteKit)
```bash
cd cryptotracker.web
npm run dev                 # Start dev server (localhost:5173, proxies to API)
npm run build               # Production build
npm run check               # TypeScript/Svelte type checking
npm run lint                # ESLint + Prettier check
npm run format              # Format with Prettier
```

### API Client Generation
```bash
make api                    # Regenerate TypeScript client from Swagger
# Requires: webapi running at localhost:5106
# Generates: cryptotracker.web/src/lib/cryptotrackerApi.ts
```

### Database Migrations (EF Core)
```bash
make ef_add_migration MigrationName    # Create new migration
make ef_update_database                 # Apply migrations
make ef_remove_migration                # Remove last migration
```

### Docker
```bash
make docker_build_web_dev   # Build dev image
make docker_build_web       # Build production image
```

## Architecture Overview

CryptoTracker is a cryptocurrency portfolio tracker with exchange integrations. It's a full-stack application:

```
┌─────────────────────────────────────────┐
│  cryptotracker.web (SvelteKit/TS)       │  Frontend - Svelte 5, Tailwind 4
└────────────────┬────────────────────────┘
                 │ API calls (generated client)
                 ▼
┌─────────────────────────────────────────┐
│  cryptotracker.webapi (ASP.NET Core)    │  REST API - JWT/OIDC auth
│    └─ UpdateService (background)        │  Scheduled balance updates
└────────────────┬────────────────────────┘
                 │
    ┌────────────┴────────────┐
    ▼                         ▼
┌──────────────┐     ┌────────────────────┐
│ cryptotracker│     │ cryptotracker      │
│ .core        │     │ .database          │
│ (logic)      │     │ (EF Core/Postgres) │
└──────────────┘     └────────────────────┘
```

### Project Dependencies
- `cryptotracker.webapi` → depends on `core` and `database`
- `cryptotracker.core` → depends on `database` (domain models)
- `cryptotracker.worker` → legacy, not actively used (UpdateService is in webapi)

### Key Domain Concepts
- **Asset**: Tracked asset (crypto, fiat, stock) identified by symbol
- **ExchangeIntegration**: Connected exchange account (Coinbase, Bitpanda, etc.) or manual entry
- **AssetMeasuring**: Time-series snapshot of holdings per asset/integration (created daily by UpdateService)

### Configuration
Runtime config is in `config/config.yml`:
- Database connection (PostgreSQL required)
- Exchange API credentials
- JWT secret (must be >32 bytes)
- OIDC provider settings
- Update interval (minutes between balance syncs)

### Tech Stack Details
- **Backend**: .NET 10.0, EF Core 10.0, CryptoClients.Net for exchange APIs
- **Frontend**: SvelteKit 2.x, Svelte 5, Tailwind CSS 4, bits-ui components
- **Testing**: NUnit, Moq, EF Core InMemory provider
- **API Client**: Auto-generated with oazapfts from OpenAPI spec

## Supported Integrations
Coinbase, Bitpanda, Crypto.com, Binance, Bitcoin (XPUB/ZPUB), Ethereum (address), XRP (address)
