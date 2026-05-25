# Local development — Docker Compose

All local testing and database setup go through **Docker Compose** and `./scripts/compose.sh`. No host .NET SDK required.

## Prerequisites

1. **Docker** (Compose v2)
2. **`app/.env`** — copy from `app/.env.example` and set `PACKAGES_TOKEN` (`read:packages`; same name as org secret)
3. **`tvs-sqlscript`** cloned as a sibling repo:

   ```text
   deladetech/
     tvs-sqlscript/    ← schema + Sql/Seeds (EF deploy)
     ZelosHR/          ← this API
   ```

   Override path: `export TVS_SQLSCRIPT_PATH=/path/to/tvs-sqlscript`

## Commands

| Command | What it does |
|---------|----------------|
| `./scripts/compose.sh test` | Unit tests in SDK container (no DB) |
| `./scripts/compose.sh migrate` | Start DB → run `tvs-db deploy` (EF + seeds) |
| `./scripts/compose.sh dev` | DB + migrate + API (recommended first run) |
| `./scripts/compose.sh reset` | `down -v` → fresh Postgres → migrate → API |
| `./scripts/compose.sh ci` | `test` + `docker build` (same as GitHub Actions) |
| `./scripts/compose.sh smoke` | HTTP checks against running API |
| `./scripts/compose.sh db` | Postgres + Redis only |

## Database (source of truth)

Schema and seeds are **not** applied from `app/src/Database/Migrations/*.sql` (legacy, disabled).

The `migrate` service runs:

```bash
# Equivalent to:
cd ../tvs-sqlscript
dotnet run --project src/Trovesuite.Database.Runner -- \
  db 5432 user password zeloshrdb deploy
```

Deploy order inside Runner: `core_platform` → `loandrift` → `mystoreguard` → `human_resource` (EF migrations + RBAC/reference `Sql/Seeds` only).

**No demo tenant or `zhr_*` rows** are inserted by deploy. For local API calls, insert matching `core_platform` context (`cp_organizations`, `cp_businesses`, `cp_locations`, `cp_business_app_locations`, `cp_users`, `cp_user_locations`) and any `zeloshr` data you need, then align `LocalDevelopment` in `appsettings.Development.json` / `app/.env` and Trove headers (see `docs/SWAGGER.md`). Production uses the real Trovesuite database as-is.

## API

After `./scripts/compose.sh dev`:

- Swagger: http://localhost:8000/swagger  
- Trove headers on every `/api/v1/*` call: `app-id`, `authorization` (Bearer JWT), `bus-id`, `loc-id`, `org-id` — see `docs/SWAGGER.md`  
- Optional JWT: see [TROVESUITE.md](TROVESUITE.md)

```bash
./scripts/compose.sh smoke
./scripts/compose.sh logs
```

## Clean slate

```bash
./scripts/compose.sh reset
```

Removes the Postgres volume, reapplies all modules from `tvs-sqlscript`, and restarts the API.

## CI parity

GitHub Actions runs:

```bash
./scripts/compose.sh test
docker build --build-arg PACKAGES_TOKEN=... -f app/Dockerfile .
```

DB migrate is **not** run in CI today (unit tests use mocks). Add an integration profile later if needed.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `tvs-sqlscript not found` | Clone repo or set `TVS_SQLSCRIPT_PATH` |
| `PACKAGES_TOKEN is not set` | Add PAT to `app/.env` as `PACKAGES_TOKEN=` |
| API 500 / missing column | `./scripts/compose.sh migrate` or `reset` |
| Port 5431 in use | Change `DB_PORT` in `app/.env` |
