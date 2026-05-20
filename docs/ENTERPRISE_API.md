# ZelosHR Enterprise API

ZelosHR is a **multi-tenant HR platform**, not a demo read API. Every business module supports full lifecycle operations scoped by `tenant_id` and `org_id` from Trovesuite auth (JWT) or explicit headers in development.

## Conventions

| Item | Rule |
|------|------|
| Base path | `/api/v1` |
| Auth | `Authorization: Bearer <jwt>` when `TrovesuiteIntegration:RequireAuthentication=true` |
| Tenant scope | `tenant_id` + `org_id` on every row; never returned cross-tenant |
| Envelope | `{ success, statusCode, detail, data, pagination?, fieldErrors? }` |
| Create | `POST` → `201` where applicable |
| Update | `PATCH` (partial body, only sent fields change) |
| Delete | `DELETE` → soft delete or archive unless noted |
| List | `GET` with `page`, `size`, filters |
| Detail | `GET /{resourceId}` |

## CRUD matrix

| Module | List | Get | Create | Update | Delete / archive |
|--------|------|-----|--------|--------|------------------|
| **Employees** | `GET /employees`, `GET /employees/directory` | `GET /employees/{id}` | `POST /employees` | `PATCH /employees/{id}`, `PATCH .../employment`, `PATCH .../lifecycle-state` | `DELETE /employees/{id}` (soft) |
| **Org — departments** | `GET /org-structure/departments` | — | `POST /org-structure/departments` | `PATCH /org-structure/departments/{id}` | `DELETE .../departments/{id}` (archive) |
| **Org — branches** | `GET /org-structure/branches` | — | `POST /org-structure/branches` | `PATCH /org-structure/branches/{id}` | `DELETE .../branches/{id}` (archive) |
| **Lifecycle events** | `GET /lifecycle-events` | `GET /lifecycle-events/{id}` | `POST /lifecycle-events` | `PATCH /lifecycle-events/{id}` | `DELETE /lifecycle-events/{id}` |
| **Audit logs** | `GET /audit-logs` | `GET /audit-logs/{id}` | — (system-written) | — | — |
| **Attendance** | `GET /attendance` | `GET /attendance/{id}` | `POST /attendance` | `PATCH /attendance/{id}` | `DELETE /attendance/{id}` |
| **Leave** | `GET /leave` | `GET /leave/requests/{id}` | `POST /leave/requests` | `PATCH /leave/requests/{id}` | `DELETE /leave/requests/{id}` (pending only) |
| **Recruitment** | `GET /recruitment` | `GET /recruitment/{id}` | `POST /recruitment` | `PATCH /recruitment/{id}` | `DELETE /recruitment/{id}` |
| **Onboarding** | `GET /onboarding` | `GET /onboarding/{id}` | `POST /onboarding` | `PATCH /onboarding/{id}` | `DELETE /onboarding/{id}` |
| **Performance** | `GET /performance` | `GET /performance/{id}` | `POST /performance` | `PATCH /performance/{id}` | `DELETE /performance/{id}` |
| **Disciplinary** | `GET /disciplinary` | `GET /disciplinary/{id}` | `POST /disciplinary` | `PATCH /disciplinary/{id}` | `DELETE /disciplinary/{id}` |
| **Documents** | `GET /documents` | `GET /documents/{id}` | `POST /documents` | `PATCH /documents/{id}` | `DELETE /documents/{id}` |

## Implementation status

See [SPRINTS.md](SPRINTS.md) for sprint ownership. **Sprint 5+** delivers write APIs; Epic 1 (Employee Core) is the priority.

## Permissions (Trovesuite)

RBAC permissions are seeded in `tvs-sqlscript` (`permission-zeloshr-*`). Wire `[Authorize]` / permission checks per endpoint in a later sprint once JWT is mandatory in all environments.
