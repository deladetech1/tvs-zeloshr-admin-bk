# Architecture audit — ZelosHR.Api

> **Baseline:** `4fedd06` (Dapper monolith)  
> **Uplift branch:** `feature/uplift` — EF Core, repositories, Trove headers (2026-05)  
> **Schema owner:** [tvs-sqlscript](https://github.com/deladetech1/tvs-sqlscript) — never extend `app/src/Database/Migrations/*.sql` for new DDL

## Executive summary

The uplift **delivered** the main architectural goals: **EF Core + repository pattern**, **tenant-scoped persistence**, **unit tests with mocks**, and **platform/Trove integration**. The API is suitable to merge against a database deployed from tvs-sqlscript (shared dev or production).

**SOLID is improved, not exhaustive.** Repositories and infrastructure depend on abstractions; most controllers still inject concrete `*Service` types because those services expose module-specific methods beyond shared interfaces.

---

## Completed (uplift)

| Area | State |
|------|--------|
| Data access | EF Core 10 + Npgsql; Dapper removed |
| Repositories | Module-specific interfaces in `Persistence/Repositories/` |
| Tenant scoping | `ITenantContext` / `TenantContextAdapter`; repos filter by tenant/org |
| Employees | `IEmployeesService` (CRUD/search); `IEmployeeLookup` (cross-module display resolve) |
| Platform | `ICpUserRepository`, `IPlatformContextRepository`; Trove header middleware |
| Tests | Service + middleware tests (xUnit, NSubstitute, FluentAssertions) |
| Schema | `RunDatabaseMigrations=false`; deploy via tvs-sqlscript only |
| Navigation | Dynamic routes from OpenAPI groups |

---

## SOLID — current assessment

| Principle | Status | Notes |
|-----------|--------|--------|
| **S** Single responsibility | Partial | Repositories vs services split is clear; `EmployeesService` / `EmployeeRegistrationService` remain large orchestrators (acceptable for one use case per class). |
| **O** Open/closed | Partial | Swappable repos/storage; new features still edit existing services (normal for this codebase size). |
| **L** Liskov substitution | OK | No broken inheritance; interface fakes work in tests. |
| **I** Interface segregation | Partial | Lean repo interfaces; `IEmployeeLookup` added for cross-module employee reads; most `*Service` types have no interface. |
| **D** Dependency inversion | Partial | Repos + infra use DI abstractions; **controllers → concrete services** except Trovesuite platform types. |

---

## Intentionally deferred (low ROI / higher risk)

- Service interfaces for every module (`ILeaveService`, …) — ceremony unless adding HTTP integration tests.
- Splitting registration wizard into many classes — high regression risk; defer until new flows.
- Controller-wide interface injection — `EmployeesController` needs legacy methods not on `IEmployeesService`.
- Domain events / outbox — not required for current scope.

---

## Removed dead abstractions

- Generic `IService<TRead,TWrite,TKey>` — unused; HR modules are not uniform CRUD.

---

## Test coverage

| Layer | Coverage |
|-------|----------|
| Unit (services, middleware, query builders) | Yes — `tests/ZelosHR.Api.Tests` |
| Integration / WebApplicationFactory | No |
| Against live Azure dev-db | Manual (Swagger + Trove headers) |

---

## Tenant & platform

- Headers: `app-id`, `authorization`, `bus-id`, `loc-id`, `org-id` (see `docs/SWAGGER.md`).
- `ValidatePlatformContext` checks `cp_business_app_locations` + `cp_user_locations` against the **live** database.
- Demo rows are **not** seeded by tvs-sqlscript deploy; align `LocalDevelopment` with real rows or insert locally.

---

## Related PRs

1. [tvs-sqlscript — EF schema + RBAC](https://github.com/deladetech1/tvs-sqlscript/pull/1) — merge/deploy first.
2. [ZelosHR API — uplift](https://github.com/deladetech1/tvs-zeloshr-bk/pull/1)

---

## Safe follow-ups (optional)

1. Narrow interfaces when a second module depends on another service (pattern: `IEmployeeLookup`).
2. Refresh `ARCHITECTURE_AUDIT.md` when major modules land.
3. Add integration tests only when stabilizing against shared dev-db.
