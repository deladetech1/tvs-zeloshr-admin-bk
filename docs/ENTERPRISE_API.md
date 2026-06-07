# ZelosHR Enterprise API

ZelosHR is a **multi-tenant HR platform**, not a demo read API. Every business module supports full lifecycle operations scoped by `tenant_id` and `org_id` from Trovesuite auth (JWT) or explicit headers in development.

## Conventions

| Item | Rule |
|------|------|
| Base path | `/api/v1` |
| Auth | `Authorization: Bearer <jwt>` when `TrovesuiteIntegration:RequireAuthentication=true` |
| Tenant scope | `tenant_id` + `org_id` on every row; never returned cross-tenant |
| Envelope | `{ success, status_code, detail, data, pagination?, field_errors? }` (snake_case; Mystoreguard-aligned — see [MYSTOREGUARD_API_CONFORMANCE.md](MYSTOREGUARD_API_CONFORMANCE.md)) |
| Resource IDs | **Query parameters only** (`employee_id`, `attendance_id`, …) — never in URL paths |
| Statistics | `GET /{module}/statistics` (KPIs; optional `from_date`, `to_date` where supported) |
| List | `GET /{module}/list` with `page`, `size`, filters |
| Get one | `GET /{module}/get?{resource}_id=` |
| Create | `POST /{module}/add` |
| Update | `PUT /{module}/update?{resource}_id=` (partial JSON body) |
| Delete | `DELETE /{module}/delete?{resource}_id=` → soft delete or archive unless noted |

## CRUD matrix (platform routes)

| Module | Statistics | List | Get | Create | Update | Delete |
|--------|------------|------|-----|--------|--------|--------|
| **Employees** | `GET /employees/statistics` | `GET /employees/list`, `GET /employees/directory` | `GET /employees/get?employee_id=` | `POST /employees/add` | `PUT /employees/update?employee_id=` (partial aggregate body) | `DELETE /employees/delete?employee_id=` |
| **Org structure** | `GET /org-structure/statistics` | `GET /org-structure/departments/list`, `GET /org-structure/branches/list` | — | `POST .../departments/add`, `.../branches/add` | `PUT .../departments/update?department_id=`, `.../branches/update?branch_id=` | `DELETE .../departments/delete?department_id=`, `.../branches/delete?branch_id=` |
| **Lifecycle events** | `GET /lifecycle-events/statistics` | `GET /lifecycle-events/list` | `GET /lifecycle-events/get?lifecycle_event_id=` | `POST /lifecycle-events/add` | `PUT /lifecycle-events/update?lifecycle_event_id=` | `DELETE /lifecycle-events/delete?lifecycle_event_id=` |
| **Audit logs** | `GET /audit-logs/statistics` | `GET /audit-logs/list` | `GET /audit-logs/get?audit_log_id=` | — (system) | — | — |
| **Attendance** | `GET /attendance/statistics` | `GET /attendance/list` | `GET /attendance/get?attendance_id=` | `POST /attendance/add` | `PUT /attendance/update?attendance_id=` | `DELETE /attendance/delete?attendance_id=` |
| **Leave** | `GET /leave/statistics` | `GET /leave/list` | `GET /leave/get?leave_request_id=` | `POST /leave/add` | `PUT /leave/update?leave_request_id=` | `DELETE /leave/delete?leave_request_id=` |
| **Recruitment** | `GET /recruitment/statistics` | `GET /recruitment/list` | `GET /recruitment/get?recruitment_id=` | `POST /recruitment/add` | `PUT /recruitment/update?recruitment_id=` | `DELETE /recruitment/delete?recruitment_id=` |
| **Onboarding** | `GET /onboarding/statistics` | `GET /onboarding/list` | `GET /onboarding/get?onboarding_id=` | `POST /onboarding/add` | `PUT /onboarding/update?onboarding_id=` | `DELETE /onboarding/delete?onboarding_id=` |
| **Performance** | `GET /performance/statistics` | `GET /performance/list` | `GET /performance/get?performance_id=` | `POST /performance/add` | `PUT /performance/update?performance_id=` | `DELETE /performance/delete?performance_id=` |
| **Disciplinary** | `GET /disciplinary/statistics` | `GET /disciplinary/list` | `GET /disciplinary/get?disciplinary_id=` | `POST /disciplinary/add` | `PUT /disciplinary/update?disciplinary_id=` | `DELETE /disciplinary/delete?disciplinary_id=` |
| **Documents** | `GET /documents/statistics` | `GET /documents/list` | `GET /documents/get?document_id=` | `POST /documents/add` | `PUT /documents/update?document_id=` | `DELETE /documents/delete?document_id=` |
| **Custom fields** | `GET /custom-fields/statistics` | `GET /custom-fields/list` | `GET /custom-fields/get?custom_field_id=` | `POST /custom-fields/add` | `PUT /custom-fields/update?custom_field_id=`, `PUT /custom-fields/reorder` | `DELETE /custom-fields/delete?custom_field_id=` |
| **Dashboard** | `GET /dashboard/statistics` | — | — | — | — | — |

Legacy read-only aliases: `GET /departments/statistics`, `GET /departments/list`; `GET /branches/list`. Hidden from Swagger: `GET /employees/directory/summary` (use `statistics`).

`GET /custom-fields/list` supports `search`, `entity_type`, filters, `page`, `size`. Also: `GET /custom-fields/entity-types`, `GET /custom-fields/sections?entity_type=`, `GET /custom-fields/schema?entity_type=`, audit routes as documented in Swagger.

`GET /employees/list` and `GET /employees/directory` support `search`, department/branch filters, `sort_by`, `sort_order`, `page`, `size`, `include_inactive`.

## Implementation status

See [SPRINTS.md](SPRINTS.md) for sprint ownership. **Sprint 5+** delivers write APIs; Epic 1 (Employee Core) is the priority.

## Permissions (Trovesuite)

RBAC permissions are seeded in `tvs-sqlscript` (`permission-zeloshr-*`). Controllers use `[RequiresZelosHrPermission(...)]` per endpoint.
