# Frontend ↔ API contracts

See [ENTERPRISE_API.md](ENTERPRISE_API.md) for the full CRUD matrix and [GET /api/v1/navigation](http://localhost:8000/api/v1/navigation) for the live route map.

## Tenancy & envelope

| Item | Rule |
|------|------|
| Auth | `authorization: Bearer <JWT>` plus `app-id`, `bus-id`, `loc-id`, `org-id` on every `/api/v1/*` request |
| Envelope | `{ success, statusCode, detail, data, pagination?, fieldErrors? }` |
| Updates | `PATCH` with partial JSON bodies |
| Deletes | `DELETE` — employees soft-delete; departments/branches archive |

---

## Employees (`/api/v1/employees`)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/directory/summary` | KPI cards |
| GET | `/directory` | Table (search, filters, pagination) |
| GET | `/directory/filter-options` | Dropdown values |
| GET | `/` | Simple list |
| GET | `/{id}` | Full profile + employment |
| POST | `/` | Create (Pre-hire) |
| PATCH | `/{id}` | Update personal fields |
| PATCH | `/{id}/employment` | Job title, dept, branch, manager, status |
| PATCH | `/{id}/lifecycle-state` | State transition |
| DELETE | `/{id}` | Soft delete |

---

## Organisation (`/api/v1/org-structure`)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/summary` | Tab counts |
| GET | `/departments` | Department table |
| POST | `/departments` | Create department |
| PATCH | `/departments/{id}` | Update department |
| DELETE | `/departments/{id}` | Archive |
| GET | `/branches` | Branch list |
| POST | `/branches` | Create branch |
| PATCH | `/branches/{id}` | Rename branch |
| DELETE | `/branches/{id}` | Archive |
| GET | `/chart` | Nested org chart |

---

## Lifecycle & audit

| Module | Write routes |
|--------|----------------|
| Lifecycle | `POST /`, `PATCH /{id}`, `DELETE /{id}` |
| Audit | Read-only: `GET /`, `GET /{id}` |

---

## Operations modules

Each module follows: `GET /summary`, `GET /`, `GET /{id}`, `POST /`, `PATCH /{id}`, `DELETE /{id}`.

| Module | Base path | Notes |
|--------|-----------|--------|
| Attendance | `/attendance` | Clock times as `HH:mm` strings on create |
| Leave | `/leave` | Requests under `/requests` |
| Recruitment | `/recruitment` | Job postings |
| Onboarding | `/onboarding` | Per-employee tasks |
| Performance | `/performance` | Review cycles |
| Disciplinary | `/disciplinary` | Cases |
| Documents | `/documents` | Metadata only until file upload sprint |

---

## Dashboard

`GET /dashboard` and `GET /dashboard/summary` — aggregated KPIs (read-only).
