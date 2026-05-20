# ZelosHR Backend

.NET 10 API for the **Employee** module, integrated with **Trovesuite.Package** (auth, email, storage).

**Database:** schema and seeds live in **[tvs-sqlscript](../tvs-sqlscript)** (.NET EF Core). See [AGENTS.md](AGENTS.md). This API does not run raw SQL migrations by default.

```bash
cd ../tvs-sqlscript && dotnet build
TVS_SEED_ZELOSHR_DEMO=1 dotnet run --project src/Trovesuite.Database.Runner -- localhost 5431 user password zeloshrdb deploy
```

## Quick start

### Option A — Docker only (no .NET SDK required)

```bash
cp app/.env.example app/.env
# Add GITHUB_PACKAGES_TOKEN=ghp_xxx to app/.env (read:packages scope)
COMPOSE_ENV_FILES=app/.env docker compose up -d --build
```

- **Swagger:** http://localhost:8000/swagger (OpenAPI v1 — all CRUD routes, JWT + tenant headers; see [docs/SWAGGER.md](docs/SWAGGER.md))  
- **Logs:** `docker compose logs -f api`

### Option B — Run API on your Mac (requires .NET 10 SDK)

```bash
# https://dotnet.microsoft.com/download/dotnet/10.0
cp app/.env.example app/.env
export GITHUB_PACKAGES_TOKEN=ghp_your_token
dotnet nuget add source "https://nuget.pkg.github.com/deladetech1/index.json" \
  --name github-deladetech1 --username deladetech1 --password "$GITHUB_PACKAGES_TOKEN" \
  --store-password-in-clear-text --configfile nuget.config
docker compose up -d db redis
dotnet restore app/ZelosHR.Api.csproj
cd app && dotnet run
```

- **Local tenancy:** `X-Tenant-Id` and `X-Org-Id` when JWT auth is off (see [docs/TROVESUITE.md](docs/TROVESUITE.md))

## Tests (TDD)

```bash
dotnet test
```

Unit tests cover query builders and formatting; run against `tests/ZelosHR.Api.Tests`.

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
