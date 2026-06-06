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
| PUT | `/update?employee_id=` | Partial update — send only changed sections (no `id` in body) |
| DELETE | `/delete?employee_id=` | Soft delete |

Link existing platform user: `POST /import` (separate from `POST /add`). Wizard: `POST /draft`, `PUT /update`, `POST /finalise?employee_id=`.

### `POST /add` and `PUT /update` body (snake_case)

#### Required vs optional — create (`POST /add`)

| Field | Required | When required | Notes |
|-------|----------|---------------|-------|
| `status` | no | — | Default `draft`. Use `finalised` to complete registration in one step. |
| `identity.full_name` | **yes** | always on create | Display name. |
| `identity.phone` | **yes** | always on create | Contact number, e.g. `+233201234567`. |
| `identity` (other fields) | no | — | See identity table below (includes optional `profile_url`). |
| `employment` | no | — | Required fields apply only when **finalising** (see below). |
| `compensation` | no | — | `currency_id` required when `gross_salary` is set and employee has no currency yet. |
| `education` / `certifications` | no | — | Arrays; empty `[]` is fine. |
| `document_ids` | no | — | From `POST /file/post/multiple`. |

**When `status` is `finalised`** (or completing a draft later), the API also requires:

| Field | Required on finalise |
|-------|----------------------|
| `identity.full_name` | yes |
| `employment.job_title` | yes |
| `employment.department_id` | yes |
| `identity.work_email` | yes (creates/links `cp_users`) |

#### Required vs optional — update (`PUT /update`)

Send **only** sections/fields you are changing. At least one top-level field or section must be present.

| Field | Notes |
|-------|-------|
| `status` | Set to `finalised` to complete a draft (same rules as finalise above). |
| `identity`, `employment`, `compensation` | Partial objects — omitted keys are left unchanged. Include `identity.profile_url` to set or clear photo. |
| `lifecycle_state` | Update only. |
| `education` / `certifications` | Include `id` to update row; omit `id` to add. |
| `delete_education_ids` / `delete_certification_ids` | UUID arrays. |
| `document_ids` / `delete_document_ids` | Append or remove file-registry IDs. |

#### Top-level body fields

| Field | Create | Update | Notes |
|-------|--------|--------|-------|
| `status` | yes | no | Create only: `draft` \| `finalised` |
| `identity` | object | optional | See identity table below |
| `employment` | optional | optional | Job, dept, branch, manager, etc. |
| `compensation` | optional | optional | Salary, SSNIT, TIN, bank |
| `lifecycle_state` | no | optional | e.g. `pre_hire`, `active`, `terminated` |
| `education` | array | optional | Items: include `id` to update, omit to add |
| `certifications` | array | optional | Same as education |
| `document_ids` | optional | optional | File-registry IDs (not profile photo) |
| `delete_education_ids` | no | optional | UUID[] |
| `delete_certification_ids` | no | optional | UUID[] |
| `delete_document_ids` | no | optional | UUID[] |

**Do not** send `import` / `existing_user_id` on `POST /add` — use `POST /import` instead.

### `identity` object

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `full_name` | **yes** (create) | string | Display name |
| `date_of_birth` | no | date | `YYYY-MM-DD` |
| `gender` | no | string | e.g. `male`, `female` |
| `country` | no | string | Country of citizenship, e.g. `Ghana` |
| `id_type` | no | string | Suggested: `ghana_card`, `passport`, `voter_id`, `drivers_license`, `ssnit`, `other` |
| `id_issue_date` | no | date | When the ID was issued (`YYYY-MM-DD`) |
| `id_expiry_date` | no | date | When the ID expires (`YYYY-MM-DD`) |
| `id_number` | no | string | The ID number matching `id_type` |
| `personal_email` | no | string | Non-work email (HR record) |
| `work_email` | no* | string | Work email; links/creates platform user (*required to finalise) |
| `phone` | **yes** (create) | string | Contact number, e.g. `+233201234567` |
| `linkedin_url` | no | string | |
| `residential_address` | no | string | |
| `profile_url` | no | string | HTTPS URL; synced to `cp_users.profile_pic` when linked. Pass `""` on update to clear. |
| `custom_fields` | no | object | Tenant-defined values; keys = admin `field_key` for section `employee-directory-identity` |

**Profile photo:** set `identity.profile_url` on create/update, or upload after save when `work_email` is linked (`POST /photo/upload` — see [TROVESUITE.md](TROVESUITE.md)).

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
