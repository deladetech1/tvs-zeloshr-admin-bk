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
| PUT | `/update` | Partial update — body includes `id` (employee UUID); send only changed sections |
| DELETE | `/delete?employee_id=` | Soft delete |

Link existing platform user: `POST /import` (separate from `POST /add`). Wizard: `POST /draft`, `PUT /update`, `POST /finalise?employee_id=`.

### `POST /add` and `PUT /update` body (snake_case)

| Field | Create | Update | Notes |
|-------|--------|--------|-------|
| `status` | yes | no | Create only: `draft` \| `finalised` |
| `id` | no | **required** | Employee UUID from `GET /get` |
| `identity` | object | optional | See identity table below |
| `employment` | optional | optional | Job, dept, branch, manager, etc. |
| `compensation` | optional | optional | Salary, SSNIT, TIN, bank |
| `lifecycle_state` | no | optional | e.g. `pre_hire`, `active`, `terminated` |
| `education` | array | optional | Items: include `id` to update, omit to add |
| `certifications` | array | optional | Same as education |
| `custom_fields` | object | optional | `{ "field_key": "value" }` |
| `delete_education_ids` | no | optional | UUID[] |
| `delete_certification_ids` | no | optional | UUID[] |

**Do not** send `import` / `existing_user_id` on `POST /add` — use `POST /import` instead.

### `identity` object

| Field | Type | Description |
|-------|------|-------------|
| `full_name` | string | Display name (required on create) |
| `date_of_birth` | date | `YYYY-MM-DD` |
| `gender` | string | e.g. `male`, `female` |
| `country` | string | Country of citizenship, e.g. `Ghana` |
| `id_type` | string | **What kind of ID** — suggested: `ghana_card`, `passport`, `voter_id`, `drivers_license`, `ssnit`, `other` |
| `id_issue_date` | date | When the ID was issued (`YYYY-MM-DD`) |
| `id_expiry_date` | date | When the ID expires (`YYYY-MM-DD`) |
| `id_number` | string | **The ID number** matching `id_type` |
| `personal_email` | string | Non-work email (HR record) |
| `work_email` | string | Work email; links/creates platform user |
| `phone` | string | Contact number |
| `linkedin_url` | string | |
| `residential_address` | string | |

**Government ID (frontend):** `id_type` + `id_number` + optional `id_issue_date` / `id_expiry_date`. Example:

```json
"id_type": "ghana_card",
"id_number": "GHA-123456789-0",
"id_issue_date": "2020-05-15",
"id_expiry_date": "2030-05-14"
```

### `education[]` items

| Field | Type | Notes |
|-------|------|--------|
| `id` | uuid | Update only — omit on create |
| `institution` | string | Required |
| `degree` | string | |
| `field_of_study` | string | |
| `start_date` | date | `YYYY-MM-DD` (not year integer) |
| `end_date` | date | `YYYY-MM-DD` |
| `is_current` | boolean | |

File uploads: `POST /photo/upload`, `POST /documents/upload`.

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
