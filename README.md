# ZelosHR Backend

.NET 10 API for the **Employee** module, integrated with **Trovesuite.Package** (auth, email, storage).

**Database:** schema and seeds live in **[tvs-sqlscript](../tvs-sqlscript)** (.NET EF Core). See [AGENTS.md](AGENTS.md). This API does not run raw SQL migrations by default.

## Quick start (Compose — recommended)

```bash
cp app/.env.example app/.env
# Set PACKAGES_TOKEN in app/.env (read:packages; same name as org secret)
# Clone tvs-sqlscript as ../tvs-sqlscript

chmod +x scripts/compose.sh scripts/compose/*.sh
./scripts/compose.sh dev      # Postgres + migrate + API
./scripts/compose.sh test     # unit tests (no DB)
./scripts/compose.sh smoke    # HTTP checks
```

- **Swagger:** http://localhost:8000/swagger  
- **Full guide:** [docs/LOCAL_DEV.md](docs/LOCAL_DEV.md)  
- **Schema:** deployed by `./scripts/compose.sh migrate` from `../tvs-sqlscript` (not legacy SQL in this repo)

### Option B — API on host (.NET 10 SDK)

```bash
./scripts/compose.sh db
./scripts/compose.sh migrate
export PACKAGES_TOKEN=ghp_your_token
dotnet nuget update source github-deladetech1 --username deladetech1 \
  --password "$PACKAGES_TOKEN" --store-password-in-clear-text --configfile nuget.config
cd app && dotnet run
```

## Tests

```bash
./scripts/compose.sh test
# CI parity:
./scripts/compose.sh ci
```

## API map (enterprise CRUD)

Every HR module exposes **list, get-by-id, create, update, and delete** (audit logs are read-only). Employees and org structure include additional assignment endpoints.

**Route discovery:** `GET /api/v1/navigation`

| Doc | Contents |
|-----|----------|
| [docs/ENTERPRISE_API.md](docs/ENTERPRISE_API.md) | Full CRUD matrix |
| [docs/API_CONTRACTS.md](docs/API_CONTRACTS.md) | Frontend contracts |
| [docs/SPRINTS.md](docs/SPRINTS.md) | Sprint roadmap |

## Structure

```text
app/src/Entities/
  employees/       # Directory + create
  departments/     # Org chart — departments
  branches/        # Org chart — branches
  audit_logs/
  lifecycle_events/
  shared/          # Health, Respons<T>
app/src/Database/Migrations/
tests/ZelosHR.Api.Tests/
```

## CI/CD

GitHub Actions deploys the API container and Azure Functions on push to `dev` / `main`. See [docs/CICD.md](docs/CICD.md).

## License

Proprietary — Deladetech / ZelosHR
