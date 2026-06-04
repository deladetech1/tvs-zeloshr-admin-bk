# Live dev API testing (local only)

Secrets stay in **gitignored** files — never commit `live-session.env`, `live-db.env`, or `.jwt-secret.local`.

## Two ways to test against **dev data**

| Mode | API runs | Database | Config |
|------|----------|----------|--------|
| **Deployed** (default) | Azure `trovesuite-dev-zeloshr-ca` | dev Postgres (already wired) | `live-session.env` only |
| **Local API + live DB** | `localhost:8000` | same dev Postgres as Container App | `live-db.env` + `live-session.env` |

### A — Hit deployed API (simplest)

```bash
cp scripts/local-dev/live-session.example.env scripts/local-dev/live-session.env
# Paste Bearer token + org/bus/loc after Trove login

./scripts/local-dev/smoke-dev.sh smoke
./scripts/local-dev/smoke-dev.sh get /api/v1/custom-fields/statistics

# Custom-fields only (ordered smoke)
./scripts/local-dev/test-custom-fields.sh
```

After JWT secret rotation, **log in again** and refresh `TROVE_BEARER_TOKEN`. Ensure `TROVE_TENANT_ID` matches the JWT claim exactly (a typo causes `403 Invalid platform context` even when org/bus/loc look correct).

### B — Local API using dev Postgres (same DB as Azure)

```bash
az login   # trovesuite-dev subscription

# Pull App__ConnectionString from Container App
./scripts/local-dev/pull-azure-dev-env.sh

# Terminal 1 — API (uses .jwt-secret.local if present)
chmod +x scripts/local-dev/*.sh
./scripts/local-dev/run-api-live-db.sh

# live-db.env — point smoke at local API:
#   ZELOSHR_API_BASE=http://localhost:8000

# Terminal 2 — after fresh login, update live-session.env token
./scripts/local-dev/smoke-dev.sh smoke
```

**Warnings for live DB**

- `App__RunDatabaseMigrations=false` — never bootstrap schema against shared dev.
- Prefer read-only testing; avoid destructive writes unless intentional.

## Files

| File | Purpose |
|------|---------|
| `live-session.env` | Bearer token, org/bus/loc, API bases |
| `live-db.env` | `APP__CONNECTION_STRING` from Azure (+ optional local API base) |
| `.jwt-secret.local` | Dev JWT signing key (same as Container App) |

## Bases

| Service | Variable |
|---------|----------|
| ZelosHR | `ZELOSHR_API_BASE` (deployed URL or `http://localhost:8000`) |
| Core platform | `CORE_PLATFORM_API_BASE` |

Refresh `TROVE_BEARER_TOKEN` after JWT secret rotation or session expiry.

## Azure resource group (dev)

`trovesuite-dev-appservers-rg` — apps `trovesuite-dev-zeloshr-ca`, `trovesuite-dev-core-platform-ca`.
