# Frontend ↔ API contracts

See [ENTERPRISE_API.md](ENTERPRISE_API.md) for the full CRUD matrix and [GET /api/v1/navigation](http://localhost:8000/api/v1/navigation) for the live route map.

## Tenancy & envelope

| Item | Rule |
|------|------|
| Auth | `authorization: Bearer <JWT>` plus `app-id`, `bus-id`, `loc-id`, `org-id` on every `/api/v1/*` request |
| Envelope | `{ success, status_code, detail, data, pagination?, field_errors? }` (snake_case — [MYSTOREGUARD_API_CONFORMANCE.md](MYSTOREGUARD_API_CONFORMANCE.md)) |
| Resource IDs | Query params only (`employee_id`, `department_id`, …) — **no** `{id}` path segments |
| Updates | `PUT` with partial JSON bodies |
| Deletes | `DELETE /{module}/delete?{resource}_id=` — employees soft-delete; departments/branches archive |
| KPIs | `GET /{module}/statistics` (not `summary` on the public surface) |

---

## Employees (`/api/v1/employees`)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/statistics` | Directory KPIs |
| GET | `/directory` | Table (search, filters, pagination) |
| GET | `/directory/filter-options` | Dropdown values |
| GET | `/list` | Platform list |
| GET | `/get?employee_id=` | Aggregate read |
| GET | `/detail?employee_id=` | Flat profile DTO |
| POST | `/add` | Create (aggregate body) |
| PUT | `/update?employee_id=` | Partial update — send only changed sections (`identity`, `employment`, `compensation`, `lifecycle_state`, `education`, `certifications`, `custom_fields`) |
| DELETE | `/delete?employee_id=` | Soft delete |

Wizard: `POST /draft`, `PUT /update?employee_id=` (partial sections), `POST /finalise?employee_id=`. File uploads: `POST /photo/upload`, `POST /documents/upload`.

---

## Organisation (`/api/v1/org-structure`)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/statistics` | Tab counts |
| GET | `/departments` | Department table |
| POST | `/departments/add` | Create department |
| PUT | `/departments/update?department_id=` | Update department |
| DELETE | `/departments/delete?department_id=` | Archive |
| GET | `/branches` | Branch list |
| POST | `/branches/add` | Create branch |
| PUT | `/branches/update?branch_id=` | Rename branch |
| DELETE | `/branches/delete?branch_id=` | Archive |
| GET | `/chart` | Nested org chart |

---

## Lifecycle & audit

| Module | Pattern |
|--------|---------|
| Lifecycle | `GET /statistics`, `GET /list`, `GET /get?lifecycle_event_id=`, `POST /add`, `PUT /update?lifecycle_event_id=`, `DELETE /delete?lifecycle_event_id=` |
| Audit | Read-only list/get + `GET /statistics` |

---

## Operations modules

Each module: `GET /statistics`, `GET /list`, `GET /get?{resource}_id=`, `POST /add`, `PUT /update?{resource}_id=`, `DELETE /delete?{resource}_id=`.

| Module | Base path | Resource id param |
|--------|-----------|-------------------|
| Attendance | `/attendance` | `attendance_id` |
| Leave | `/leave` | `leave_request_id` |
| Recruitment | `/recruitment` | `recruitment_id` |
| Onboarding | `/onboarding` | `onboarding_id` |
| Performance | `/performance` | `performance_id` |
| Disciplinary | `/disciplinary` | `disciplinary_id` |
| Documents | `/documents` | `document_id` |
| Custom fields | `/custom-fields` | `custom_field_id` |
| Dashboard | `/dashboard` | — (`GET /statistics` only) |

---

## Breaking changes (Mystoreguard alignment)

- No `PATCH` — use `PUT …/update?…`
- No path UUIDs — use `?employee_id=` (etc.)
- Public KPI route is `/statistics`, not `/summary`
