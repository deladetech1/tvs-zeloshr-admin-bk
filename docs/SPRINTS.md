# ZelosHR API — Sprint map (enterprise HR platform)

## Current API surface

All business modules support **list + summary + get-by-id + create + update + delete** (audit logs are read-only). See [ENTERPRISE_API.md](ENTERPRISE_API.md) for the full matrix.

| Sidebar | Base route | CRUD |
|---------|------------|------|
| Dashboard | `/api/v1/dashboard` | Read-only KPIs |
| Employee → Directory | `/api/v1/employees` | Full CRUD + `/employment`, `/lifecycle-state` |
| Employee → Org Chart | `/api/v1/org-structure` | Departments & branches CRUD + chart |
| Employee → Lifecycle Events | `/api/v1/lifecycle-events` | Full CRUD |
| Employee → Audit Logs | `/api/v1/audit-logs` | Read-only (append by system) |
| Attendance | `/api/v1/attendance` | Full CRUD |
| Leave | `/api/v1/leave` | Requests CRUD under `/requests` |
| Recruitment | `/api/v1/recruitment` | Full CRUD (job postings) |
| Onboarding | `/api/v1/onboarding` | Full CRUD (tasks) |
| Performance | `/api/v1/performance` | Full CRUD (reviews) |
| Disciplinary | `/api/v1/disciplinary` | Full CRUD (cases) |
| Document | `/api/v1/documents` | Full CRUD (metadata; file upload later) |

**Discovery:** `GET /api/v1/navigation` — route map for frontend.

**Legacy aliases:** `/api/v1/departments`, `/api/v1/branches` (read-focused; prefer org-structure for writes).

## Auth & tenancy

- Production: `TrovesuiteIntegration:RequireAuthentication=true` + Bearer JWT (`user_id`, `tenant_id` claims).
- Local dev: optional `X-Tenant-Id` / `X-Org-Id` when auth is disabled.
- RBAC: `permission-zeloshr-*` in `tvs-sqlscript`; wire permission checks per endpoint in a follow-up sprint.

## Local database

Schema and reference data: **tvs-sqlscript** (see [AGENTS.md](../AGENTS.md)).

Optional sprint seed rows (`demo-tenant` / `demo-org`) for UI dev only:

```bash
TVS_SEED_ZELOSHR_DEMO=1 dotnet run --project src/Trovesuite.Database.Runner -- localhost 5431 user password zeloshrdb deploy
```

## Next sprints (enterprise hardening)

| Sprint | Focus |
|--------|--------|
| 6 | Trovesuite permission attributes on all mutating routes |
| 7 | Multipart document upload (Azure Blob via Trovesuite) |
| 8 | Leave approval workflow rules + balance auto-update |
| 9 | Compensation bands, pay runs (new tables in tvs-sqlscript) |
| 10 | OpenAPI examples + contract tests per module |
