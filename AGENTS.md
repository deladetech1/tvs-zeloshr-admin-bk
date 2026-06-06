# ZelosHR — Agent instructions

Read this before changing the database, seeds, or anything that affects PostgreSQL schema or reference data.

## Git branch policy (ZelosHR + tvs-sqlscript)

**Active ZelosHR work uses `dev` only** in both repos. Do not branch from or PR to `main` / `master` unless the user explicitly asks.

| Repo | Path | Work branch | PR target | Auto-deploy |
|------|------|-------------|-----------|-------------|
| **ZelosHR.Api** | this repo | `dev` | `dev` | dev Container App |
| **tvs-sqlscript** | `../tvs-sqlscript` | `dev` | `dev` | `saas-dev` Postgres |

Before starting a task:

```bash
cd /path/to/ZelosHR && git checkout dev && git pull
cd ../tvs-sqlscript && git checkout dev && git pull
```

- Schema PRs in **tvs-sqlscript** merge to **`dev` first**; let CI deploy to `saas-dev` before merging dependent ZelosHR API changes.
- `main` / `master` on either repo are release paths — not the default sprint branch.

## Database source of truth

**All database changes live in the sibling repo [tvs-sqlscript](https://github.com/deladetech1/tvs-sqlscript)** (`../tvs-sqlscript`).

This repo (**ZelosHR.Api**) does **not** own production schema. Do **not** add or rely on raw `.sql` migration scripts here.

| Change type | Where to implement (tvs-sqlscript) |
|-------------|-----------------------------------|
| New table / column / index | `Entities/` + `Configurations/` + `dotnet ef migrations add` in `Trovesuite.Database.HumanResource` |
| RBAC resource types, permissions, roles | `Seeds/HumanResourceRbacSeedData.cs` (EF seeder) |
| Demo / sprint **data rows** | **Not** in tvs-sqlscript — insert locally or use shared dev/staging DB |
| Platform HR link (`human_resource.hr_employees`) | EF entity `Employee` + existing `Initial` migration |

**Never** ship DDL or production seeds only in this repo.

**Do not run migrations from this repo or from local-dev scripts against shared dev/staging/prod Postgres.** Implement the change in **tvs-sqlscript** (EF migration + entity/config + seeds if needed), merge there, and let the normal **tvs-sqlscript** deploy pipeline apply it. ZelosHR.Api only consumes the schema that pipeline has already applied.

| You need… | Do this |
|-----------|---------|
| New column, table, index, FK | `tvs-sqlscript` → `dotnet ef migrations add` → PR → CI deploy |
| New RBAC / reference row that ships to all envs | `tvs-sqlscript` → `Seeds/` |
| Demo tenant rows for local testing | Local compose DB or manual insert — **not** tvs-sqlscript, **not** shared `dev-db` from an agent |

## tvs-sqlscript (.NET 10 / EF Core)

```
tvs-sqlscript/src/Trovesuite.Database.HumanResource/
├── Entities/              # Employee (platform) + Zhr* (zeloshr app)
├── Configurations/        # Fluent API
├── Migrations/            # EF Core migrations (source of truth for DDL)
│   ├── 20260516195150_Initial.cs
│   ├── 20260520095923_ZelosHrAppTables.cs
│   ├── 20260605120000_ScopeZhrEmployeeCodeUniqueByTenant.cs
│   └── 20260606130000_AddZhrBranchLocationFields.cs
├── Seeds/                 # RBAC reference data (EF seeder)
└── HumanResourceModule.cs
```

**ZelosHR.Api runtime config:** `app/appsettings.json` and environment-specific files — see `docs/APPCONFIG.md`. Do not rely on loose env vars not documented there.

**Schemas:**

| Schema | Contents |
|--------|----------|
| `core_platform` | Trovesuite auth (module 1) |
| `human_resource` | `hr_employees` → `cp_users` |
| `zeloshr` | `zhr_*` tables used by ZelosHR.Api |

**Deploy:**

```bash
cd ../tvs-sqlscript
dotnet build
dotnet run --project src/Trovesuite.Database.Runner -- localhost 5431 user password zeloshrdb deploy
```

Deploy order across modules: `core_platform` → `loandrift` → `mystoreguard` → `human_resource`.

See `tvs-sqlscript/README.md` for CI dispatch, rollback, and validate.

## This repo (ZelosHR.Api)

- **No raw SQL migrations** under `app/src/Database/Migrations/` for new work. Legacy files may remain for reference but must not be extended.
- **`SchemaInitializer` is disabled by default** (`App__RunDatabaseMigrations=false`). The API expects the database to already be deployed via tvs-sqlscript.
- For local-only emergency bootstrap, set `App__RunDatabaseMigrations=true` in `app/.env` — not for production.

## Agent workflow

1. Implement schema/data changes in **tvs-sqlscript** first.
2. Add or update EF entities and `IEntityTypeConfiguration<T>` classes.
3. Run `dotnet ef migrations add <Name> --project src/Trovesuite.Database.HumanResource --startup-project src/Trovesuite.Database.Runner`.
4. Update `Sql/Seeds/` when RBAC or reference data changes.
5. Do **not** add demo tenant / employee inserts to tvs-sqlscript; test data is local-only (SQL/pgAdmin) or comes from the real environment.
6. Open a PR in **tvs-sqlscript** targeting **`dev`**; link from the ZelosHR PR (also targeting **`dev`**) if both repos change.
7. **Merge tvs-sqlscript `dev` first** — that push auto-deploys schema to **`saas-dev`**. Then merge ZelosHR `dev`.
8. **Update Swagger in the same PR** — every API/DTO/route/workflow change must touch the matching files under `app/src/Configs/Swagger*.cs` and controller XML docs (see [docs/SWAGGER.md](docs/SWAGGER.md)). No exceptions.
9. Verify locally:

   ```bash
   ./scripts/compose.sh migrate   # tvs-sqlscript deploy into compose Postgres
   ./scripts/compose.sh test
   ./scripts/compose.sh dev       # or reset for a clean DB
   ./scripts/compose.sh smoke
   ```

   See [docs/LOCAL_DEV.md](docs/LOCAL_DEV.md).

## What counts as a database change

- Tables, columns, indexes, constraints
- Seed / demo rows for any `zhr_*` table in **tvs-sqlscript** (local inserts only)
- Resource types, permissions, roles for ZelosHR
- Changes to `demo-tenant` / `demo-org` test data

## PR checklist

- [ ] EF migration added/updated in **tvs-sqlscript**
- [ ] RBAC seeds updated (`01`–`03`) if permissions changed
- [ ] `dotnet build` passes in tvs-sqlscript
- [ ] Deploy tested (`deploy` — schema + reference seeds only)
- [ ] ZelosHR API smoke-tested (Trove headers: `app-id`, `authorization`, `bus-id`, `loc-id`, `org-id`)
- [ ] No new `.sql` files added under ZelosHR `app/src/Database/Migrations/`
- [ ] **Swagger kept in sync (MUST):** same PR updates `SwaggerExamples`, `SwaggerSchemaExamplesFilter`, relevant `Swagger*OperationFilter` / `SwaggerQueryParameterExamplesFilter`, controller XML, and `SwaggerConfiguration` workflow text when request/response shapes, params, routes, or examples change; verify `/swagger` locally; `GET /api/v1/navigation` and `/swagger/v1/swagger.json` list the same routes (~55 paths); Swashbuckle.AspNetCore 10.x; see [docs/SWAGGER.md](docs/SWAGGER.md)

## Related docs

- [docs/TROVESUITE.md](docs/TROVESUITE.md) — Trovesuite.Package, NuGet token
- [docs/SPRINTS.md](docs/SPRINTS.md) — API modules
- [docs/API_CONTRACTS.md](docs/API_CONTRACTS.md) — HTTP contracts
