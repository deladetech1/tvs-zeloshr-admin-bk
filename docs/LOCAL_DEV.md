# Local development — Docker Compose

All local testing and database setup go through **Docker Compose** and `./scripts/compose.sh`. No host .NET SDK required.

## Prerequisites

1. **Docker** (Compose v2)
2. **`app/.env`** — copy from `app/.env.example` and set `GITHUB_PACKAGES_TOKEN` (`read:packages`)
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
TVS_SEED_ZELOSHR_DEMO=1 dotnet run --project src/Trovesuite.Database.Runner -- \
  db 5432 user password zeloshrdb deploy
```

Deploy order inside Runner: `core_platform` → `loandrift` → `mystoreguard` → `human_resource` (includes `zeloshr` + `Sql/Seeds`).

Demo rows (`demo-tenant` / `demo-org`) load when `TVS_SEED_ZELOSHR_DEMO=1` (default in compose).

## API

After `./scripts/compose.sh dev`:

- Swagger: http://localhost:8000/swagger  
- Demo headers: `X-Tenant-Id: demo-tenant`, `X-Org-Id: demo-org`  
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
docker build --build-arg GITHUB_PACKAGES_TOKEN=... -f app/Dockerfile .
```

DB migrate is **not** run in CI today (unit tests use mocks). Add an integration profile later if needed.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `tvs-sqlscript not found` | Clone repo or set `TVS_SQLSCRIPT_PATH` |
| `GITHUB_PACKAGES_TOKEN is not set` | Add PAT to `app/.env` |
| API 500 / missing column | `./scripts/compose.sh migrate` or `reset` |
| Port 5431 in use | Change `DB_PORT` in `app/.env` |
