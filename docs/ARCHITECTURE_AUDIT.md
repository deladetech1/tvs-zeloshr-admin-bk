# Architecture audit — ZelosHR.Api (baseline: `4fedd06` / demo sprint)

> **Audit date:** 2026-05-20  
> **Branch for uplift work:** `feature/uplift` (keep `main` / `demo/freeze` for demos)

## Executive summary

The API is a **working demo-ready** .NET 10 monolith with **Dapper + raw SQL** across all HR modules. The uplift spec targets **EF Core + repository pattern + strict SOLID**. That is the right direction but is a **multi-phase migration**, not a single refactor — schema remains in **tvs-sqlscript**; this repo must not add EF migrations or `.sql` ownership here.

---

## SOLID violations (current)

| Principle | Where | Finding |
|-----------|--------|---------|
| **S** | `EmployeesService`, `*Service` in `Entities/*` | Services mix validation, SQL, mapping, and orchestration in one class (~500+ lines for employees). |
| **O** | All `*Service` classes | Closed to extension: new query shapes require editing service SQL strings. |
| **L** | N/A | Few inheritance hierarchies; low risk today. |
| **I** | DI registration | `AddEntityServices()` registers concrete `*Service` types only — **no repository or service interfaces**. |
| **D** | Controllers → `EmployeesService` | Controllers depend on **concrete** services (`EmployeesService`, `EmployeesDirectoryService`), not abstractions. |

---

## Interfaces: exist vs missing

| Area | Exists | Missing (per target architecture) |
|------|--------|-------------------------------------|
| Tenant | `ITenantContextAccessor`, `TenantContext` | `ITenantContext` abstraction (thin wrapper acceptable) |
| Auth | Trovesuite.Package `IAuthService`, middleware | `ICurrentUserService` only if not provided by package |
| Repositories | — | `IRepository<T>`, `IEmployeeRepository`, module repos |
| Services | — | `IService<TRead,TWrite,TKey>`, `IEmployeesService`, etc. |
| Result | `Respons<T>` with `Ok` / `Fail` / `ValidationError` | `NotFound` / `Forbidden` helpers; optional `Message` alias |
| Controller mapping | Manual `StatusCode(result.StatusCode, result)` | `ToActionResult()` extension |

---

## Test coverage (rough)

| Metric | Value |
|--------|--------|
| Test project | `tests/ZelosHR.Api.Tests` (xUnit) |
| Test files | **4** (query builder, name formatting, department summary, demo data constants) |
| Service tests | **0** — no `EmployeesServiceTests`, no repository tests |
| Integration / API tests | **0** |
| Mocking | **No** NSubstitute / FluentAssertions in `.csproj` yet |

Tests today validate **SQL string builders and formatting**, not service behaviour or tenant scoping.

---

## Entity modules — CRUD completeness

| Module | List | Get | Create | Update | Delete | Notes |
|--------|------|-----|--------|--------|--------|-------|
| Employees | ✓ | ✓ | ✓ | PATCH (profile, employment, lifecycle) | Soft delete | + directory sub-routes |
| Departments | ✓ | ✓ | ✓ | PATCH | DELETE | Legacy controller + org-structure |
| Branches | ✓ | ✓ | ✓ | PATCH | DELETE | Same |
| Org structure | ✓ | — | POST/PATCH departments & branches | — | Aggregated reads |
| Attendance | ✓ | ✓ | ✓ | PATCH | DELETE | Dapper |
| Leave | ✓ | ✓ | ✓ | PATCH | DELETE | Under `/requests` |
| Lifecycle events | ✓ | ✓ | ✓ | PATCH | DELETE | |
| Audit logs | ✓ | ✓ | — | — | — | Read-only (correct) |
| Recruitment, Onboarding, Performance, Disciplinary, Documents | ✓ | ✓ | ✓ | PATCH | DELETE | Dapper |
| Dashboard | ✓ | — | — | — | — | Read aggregates |
| Platform (Trovesuite) | — | — | POST auth/notify | — | — | Package-backed |
| Health / Navigation | ✓ | — | — | — | — | Navigation is **static** list, not `IApiDescriptionGroupCollectionProvider` |

---

## `Respons<T>` usage

- **Used consistently** on HR endpoints as JSON envelope (`success`, `statusCode`, `detail`, `data`, `pagination`, `fieldErrors`).
- **Gap vs spec:** property names use `Detail` / `Error` / `FieldErrors`, not `Message` / `Errors`; no `NotFound()` / `Forbidden()` factories.
- **Control flow:** `ResponseException` exists; most services return `Respons.Fail` instead of throwing (good).

---

## Tenant scoping

- **Middleware:** `TrovesuiteAuthMiddleware` + `ITenantContextAccessor` (headers or JWT claims; defaults `demo-tenant` / `demo-org`).
- **Controllers:** Pass `ctx.TenantId` / `ctx.OrgId` into services explicitly.
- **Services:** SQL includes `tenant_id` / `org_id` filters in employee paths reviewed — **pattern is manual per query**, not enforced by a repository base class.
- **Risk:** New queries can omit tenant filter; no single `IQueryable` global filter (EF would enable that later).

---

## Domain events

- **None.** No `IDomainEvent`, no outbox, no lifecycle event → employee status sync abstraction.

---

## Data access vs uplift rules

| Rule (uplift spec) | Current state |
|--------------------|---------------|
| EF Core only, no Dapper | **Violated** — `Dapper` + `Npgsql` connection per operation |
| Repositories only touch `DbContext` | **Not started** — no `DbContext` in project |
| No SQL in this repo for schema | **Violated for dev** — `app/src/Database/Migrations/*.sql` still present (gated off via `RunDatabaseMigrations=false`) |
| Schema in tvs-sqlscript | **Documented** in AGENTS.md; aligns when migrations disabled |

**Recommended phases:**

1. **Foundations** — `Respons` extensions, abstractions, exception middleware, test packages (no Dapper removal).
2. **EF infrastructure** — `ZelosHrDbContext`, entity types mapped to `zeloshr.zhr_*` (coordinate column shapes with tvs-sqlscript EF migration `ZelosHrAppTables`).
3. **Vertical slices** — Employees first: repository + service interfaces, swap Dapper implementation for EF behind `IEmployeeRepository`.
4. **Repeat** departments → branches → audit (read-only) → lifecycle.

---

## Swagger / navigation

- Swashbuckle **10.x**, OpenAPI 3.0.3 compat middleware, **~55 paths**.
- `NavigationController` returns a **hand-maintained** `NavigationMapResponse` — must be updated when routes change (Section 9 target: dynamic discovery).

---

## Packages & framework

- **.NET 10**, `Trovesuite.Package` 1.0.0, Swashbuckle 10, Dapper 2.1.66.
- Test project references **Microsoft.AspNetCore.Mvc.Testing 8.0.11** — should align to 10.x when adding integration tests.

---

## Next actions (ordered)

1. `docs: architecture audit` (this file) on `feature/uplift`.
2. `refactor(shared):` foundations (Section 3).
3. `test(employees):` red tests + NSubstitute/FluentAssertions.
4. EF `DbContext` + `Employee` entity mapping to `zeloshr.zhr_employees`.
5. `feat(employees):` repository + service behind interfaces; keep route contracts stable for demo.
