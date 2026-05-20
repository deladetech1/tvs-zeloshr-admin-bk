# ZelosHR — Agent instructions

Read this before changing the database, seeds, or anything that affects PostgreSQL schema or reference data.

## Database source of truth

**All database changes live in the sibling repo [tvs-sqlscript](https://github.com/deladetech1/tvs-sqlscript)** (`../tvs-sqlscript`).

This repo (**ZelosHR.Api**) does **not** own production schema. Do **not** add or rely on raw `.sql` migration scripts here.

| Change type | Where to implement (tvs-sqlscript) |
|-------------|-----------------------------------|
| New table / column / index | `Entities/` + `Configurations/` + `dotnet ef migrations add` in `Trovesuite.Database.HumanResource` |
| RBAC resource types | `Sql/Seeds/01_resource_types.sql` (embedded in .NET project) |
| RBAC permissions | `Sql/Seeds/02_permissions.sql` |
| RBAC roles | `Sql/Seeds/03_roles.sql` |
| Sprint / demo rows (`demo-tenant`, `demo-org`) | `Sql/Seeds/05_zeloshr_demo.sql` (runs when `TVS_SEED_ZELOSHR_DEMO=1`) |
| Platform HR link (`human_resource.hr_employees`) | EF entity `Employee` + existing `Initial` migration |

**Never** ship DDL or production seeds only in this repo.

## tvs-sqlscript (.NET 10 / EF Core)

```
tvs-sqlscript/src/Trovesuite.Database.HumanResource/
├── Entities/              # Employee (platform) + Zhr* (zeloshr app)
├── Configurations/        # Fluent API
├── Migrations/            # EF Core migrations (source of truth for DDL)
│   ├── 20260516195150_Initial.cs
│   └── 20260520095923_ZelosHrAppTables.cs
├── Sql/Seeds/             # Idempotent INSERTs (embedded resources)
└── HumanResourceModule.cs
```

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
# Sprint demo data (all sidebar modules):
TVS_SEED_ZELOSHR_DEMO=1 dotnet run --project src/Trovesuite.Database.Runner -- localhost 5431 user password zeloshrdb deploy
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
5. Put demo/sprint data in `05_zeloshr_demo.sql` (or a new numbered seed file); keep inserts idempotent (`ON CONFLICT DO NOTHING`).
6. Open a PR in **tvs-sqlscript**; link from ZelosHR PR if both repos change.
7. Verify: deploy with tvs-sqlscript, then start ZelosHR API against the same database.

## What counts as a database change

- Tables, columns, indexes, constraints
- Seed / demo rows for any `zhr_*` table
- Resource types, permissions, roles for ZelosHR
- Changes to `demo-tenant` / `demo-org` test data

## PR checklist

- [ ] EF migration added/updated in **tvs-sqlscript**
- [ ] Seeds updated (`01`–`03`, and `05` if demo data changed)
- [ ] `dotnet build` passes in tvs-sqlscript
- [ ] Deploy tested (`deploy` + optional `TVS_SEED_ZELOSHR_DEMO=1`)
- [ ] ZelosHR API smoke-tested (`X-Tenant-Id: demo-tenant`, `X-Org-Id: demo-org`)
- [ ] No new `.sql` files added under ZelosHR `app/src/Database/Migrations/`
- [ ] **Swagger kept in sync:** `GET /api/v1/navigation` and `GET /swagger/v1/swagger.json` list the same routes (~55 paths); use `Swashbuckle.AspNetCore` 10.x on .NET 10; `ApiExplorerSettings(GroupName)` is the UI **tag** only (doc id stays `v1` via `DocInclusionPredicate`); add XML `<summary>` on new controllers (see [docs/SWAGGER.md](docs/SWAGGER.md))

## Related docs

- [docs/TROVESUITE.md](docs/TROVESUITE.md) — Trovesuite.Package, NuGet token
- [docs/SPRINTS.md](docs/SPRINTS.md) — API modules
- [docs/API_CONTRACTS.md](docs/API_CONTRACTS.md) — HTTP contracts
