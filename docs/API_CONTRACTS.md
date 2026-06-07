# Frontend ↔ API contracts

See [ENTERPRISE_API.md](ENTERPRISE_API.md) for the full CRUD matrix and [GET /api/v1/navigation](http://localhost:8000/api/v1/navigation) for the live route map.

## Tenancy & envelope

| Item | Rule |
|------|------|
| Auth | `authorization: Bearer <JWT>` plus `app-id`, `bus-id`, `loc-id`, `org-id` on every `/api/v1/*` request |
| Envelope | `{ success, status_code, detail, data, pagination?, field_errors? }` (snake_case — [MYSTOREGUARD_API_CONFORMANCE.md](MYSTOREGUARD_API_CONFORMANCE.md)) |
| Resource IDs | Query params only (`employee_id`, `department_id`, …) — **no** `{id}` path segments |
| Updates | `PUT` with partial JSON bodies |
| Deletes | `DELETE /{module}/delete?{resource}_id=` — employees soft-delete; departments/branches permanent delete |
| KPIs | `GET /{module}/statistics` (not `summary` on the public surface) |

---

## Employees (`/api/v1/employees`)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/statistics` | Directory KPIs |
| GET | `/directory` | Table (search, filters, pagination) |
| GET | `/directory/filter-options` | Dropdown values |
| GET | `/list` | Platform list |
| GET | `/get?employee_id=` | Aggregate read (`documents[]`, `identity.profile_url` as objects) |
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
| `identity.work_email` | yes (creates/links `cp_users`) |

All `employment` fields (`job_title`, `department_id`, `branch_id`, `work_arrangement`, etc.) are **optional**.
When `department_id` or `branch_id` is sent, the ID must exist in org structure.
`work_arrangement: remote` with a `branch_id` is rejected (inconsistent data).

#### Database alignment (`zhr_employees`)

API-optional employment fields map to **nullable** Postgres columns: `job_title`, `department_id`, `branch_id`, `work_arrangement`, `contract_type`, etc.

Server-set NOT NULL columns (populated on insert, not required in the request body):

| Column | Set by API on insert |
|--------|----------------------|
| `employee_code` | Generated |
| `tenant_id`, `org_id` | Trove headers |
| `full_name` | `identity.full_name` (may be cleared after `cp_users` link) |
| `lifecycle_state`, `lifecycle_status`, `is_draft` | Draft defaults |
| `employment_status` | `Draft` until finalise |
| `custom_fields_data`, `document_ids` | `{}` / `[]` |

#### Required vs optional — update (`PUT /update`)

Send **only** sections/fields you are changing. At least one top-level field or section must be present.

| Field | Notes |
|-------|-------|
| `status` | Set to `finalised` to complete a draft (same rules as finalise above). |
| `identity`, `employment`, `compensation` | Partial objects — omitted keys are left unchanged. Include `identity.profile_url` to set or clear photo. |
| `lifecycle_state` | Update only. |
| `education` / `certifications` | Array items: include `id` from GET to update; omit to add. `sync_*` + full array to replace. |
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
| `education` | array | optional | Each item has `id` on read; include `id` on update |
| `certifications` | array | optional | Same as education |
| `sync_education` | no | optional | Default `false`. `true` + full `education` = replace |
| `sync_certifications` | no | optional | Default `false`. `true` + `certifications` = full replace |
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
| `profile_url` | no | string (write) / object (read) | **Write:** document id from `POST /api/v1/file/post/multiple`. **Read:** MyStoreGuard `DocumentReadDto` (`doc_id`, `name`, `presigned_url`, `description`). Stored on `cp_users.profile_pic` when linked. Pass `""` on update to clear. |
| `custom_fields` | no | object | Tenant-defined values; keys = admin `field_key` for section `employee-directory-identity` |

**Profile photo (MyStoreGuard pattern):** upload via `POST /api/v1/file/post/multiple`, then set `identity.profile_url` to the returned document id. On read, GET returns `DocumentReadDto` (same shape as product `documents[]`). Direct blob HTTPS URLs are rejected on write. Legacy rows that already store a full URL still return that URL on read.

**Attachments on read:** `GET /employees/get` returns `documents[]` (not `document_ids`) — each item is `DocumentReadDto`: `doc_id`, `name`, `presigned_url`, `description`. **Write** still uses `document_ids` string array (same as MyStoreGuard products). Full flows, curl, and errors: [FILE_MANAGEMENT.md](FILE_MANAGEMENT.md).

**Government ID (frontend):** `id_type` + `id_number` + optional `id_issue_date` / `id_expiry_date`. Example:

```json
"id_type": "ghana_card",
"id_number": "GHA-123456789-0",
"id_issue_date": "2020-05-15",
"id_expiry_date": "2030-05-14"
```

### `education[]` / `certifications[]` (same rules)

Both stay **arrays of sub-records** on GET and PUT. Identity/employment/compensation stay single objects.

**GET** (`/employees/get`) — each array item includes `id`, row fields, and `custom_fields` (employee is scoped by `?employee_id=`):

```json
"education": [{
  "id": "55555555-5555-5555-5555-555555555501",
  "institution": "University of Ghana",
  "degree": "BSc",
  "field_of_study": "Computer Science",
  "start_date": "2008-09-01",
  "end_date": "2012-06-30",
  "is_current": false,
  "custom_fields": { "honors": "First Class" }
}],
"certifications": [{
  "id": "66666666-6666-6666-6666-666666666601",
  "name": "Masters in react",
  "issuing_body": "Udemy",
  "issue_date": "2026-05-31",
  "credential_url": "https://udemy.com/certificate/3424-3424",
  "custom_fields": {}
}]
```

**PUT** — same row fields as GET. Include `id` from GET to update; omit `id` to add.

| Item field | Education | Certifications |
|------------|-----------|----------------|
| `id` | From GET — include to **update**; omit to **add** | Same |
| Required field | `institution` | `name` |
| Remove row | `delete_education_ids` | `delete_certification_ids` |
| Replace section | `sync_education: true` + full array | `sync_certifications: true` + full array |

**Bulk edit:** send only the rows you changed, each with `id` + full row from GET.

#### `education[]` item fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `id` | uuid | update only | Omit on add |
| `institution` | string | **yes** | |
| `degree` | string | no | |
| `field_of_study` | string | no | |
| `start_date` | date | no | `YYYY-MM-DD` |
| `end_date` | date | no | `YYYY-MM-DD` |
| `is_current` | boolean | no | |
| `custom_fields` | object | no | Section `employee-directory-education` |

#### `certifications[]` item fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `id` | uuid | update only | Omit on add |
| `name` | string | **yes** | Certification title |
| `issuing_body` | string | no | |
| `issue_date` | date | no | `YYYY-MM-DD` |
| `expiry_date` | date | no | `YYYY-MM-DD` |
| `credential_url` | string | no | Verification link |
| `custom_fields` | object | no | Section `employee-directory-certification` |

**Example — update one certification in bulk save:**
```json
{
  "certifications": [{
    "id": "66666666-6666-6666-6666-666666666601",
    "name": "Masters in react fundamentals",
    "issuing_body": "Udemy",
    "issue_date": "2026-05-31",
    "credential_url": "https://udemy.com/certificate/3424-3424"
  }]
}
```

See [FILE_MANAGEMENT.md](FILE_MANAGEMENT.md) for uploads (`POST /file/post/multiple`), attachments (`document_ids` / `documents[]`), and profile photos (`identity.profile_url`).

---

## File management (`/api/v1/file`)

Full guide: **[FILE_MANAGEMENT.md](FILE_MANAGEMENT.md)** — MyStoreGuard shapes, upload flow, `blob_paths`, and error reference.

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/post/multiple` | Upload files → `data[].id` |
| GET | `/list?document_ids=` | Presigned URLs (`id`, `file_name`, …) |
| PUT | `/put?document_id=` | Replace file content |
| DELETE | `/delete?document_id=` | Remove blob + registry row |

**Employee wiring:** write `document_ids` + `identity.profile_url` (strings) · read `documents[]` + `identity.profile_url` (`DocumentReadDto`).

---

## Organisation (`/api/v1/org-structure`)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/statistics` | Tab counts |
| GET | `/departments` | Department table |
| POST | `/departments/add` | Create department (`name`, optional `parent_department_id`, `head_of_department_id`, `description`) |
| PUT | `/departments/update?department_id=` | Update department (partial) |
| DELETE | `/departments/delete?department_id=` | Permanently delete (409 if employees assigned or child departments exist) |
| GET | `/branches` | Branch list |
| POST | `/branches/add` | Create branch (`name`, optional `address`, `country`, `description`) |
| PUT | `/branches/update?branch_id=` | Update branch (partial: name, address, country, description) |
| DELETE | `/branches/delete?branch_id=` | Permanently delete (409 if employees assigned) |
| GET | `/chart` | Nested org chart (`data.roots[]` with `id`, `name`, `node_type`, `parent_id`, `head_of_department`, `employee_count`, `children`) |

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
- Employee read attachments: `documents[]` / `doc_id` (not `document_ids[]` / `id`) — see [FILE_MANAGEMENT.md](FILE_MANAGEMENT.md)
