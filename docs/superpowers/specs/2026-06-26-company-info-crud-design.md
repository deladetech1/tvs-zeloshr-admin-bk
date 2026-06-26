# Company Info — Profile + Offices CRUD

**Date:** 2026-06-26
**Status:** Approved for planning
**Swagger group:** `Company Settings` (existing — shared with `id-card-types`, `employment-types`)

## Goal

Add backend CRUD for the "Company information" settings page: a per-org **Company Profile**
(legal identity, registration, contact, logo/banner) and a per-org **Offices** list (with a
`headoffice` flag), matching the provided mockup. Two repos are involved per `AGENTS.md`:
schema in `tvs-sqlscript` (EF Core, source of truth for DDL), API in this repo (`ZelosHR.Api`).

## Scope decisions (confirmed with stakeholder)

| Decision | Choice |
|---|---|
| Resources in scope | Both Company Profile and Offices |
| Company Profile CRUD shape | True CRUD — explicit `POST`/`GET`/`PUT`/`DELETE` (not upsert-only), even though it's logically 0/1 per org |
| **Update shape (revised 2026-06-26)** | `PUT /update` is the **same shape as `POST /add`, plus `id`** — a full replacement, not a partial patch. `id` must match the profile's current id (from `GET /get`); mismatch or missing → `400`. Omitted optional fields are cleared, exactly like create. The one exception is `offices`, which keeps its own "absent = untouched" semantics (see below). |
| Head-office uniqueness | **Not enforced.** `is_head_office` is a plain client-managed boolean; any number (incl. zero) of offices may be flagged; any office, head or not, can be deleted |
| Logo / banner | In scope now, via the **existing** File Management module (no new upload endpoint) |
| Delete cascade | `DELETE /company/info/delete` also deletes all of that org's offices, in one transaction |
| Routes / permissions | Match existing convention exactly — verb-suffix routes, reuse `EmployeeGet`/`EmployeeUpdate` permissions, no new RBAC seed |
| Office fields | `name`, `country`, `city`, `phone`, `is_head_office` only — no street address (not in mockup, YAGNI) |
| **Office API shape (revised 2026-06-26)** | **Offices are not an independent CRUD resource and have no dedicated routes at all** — not even a single-office update. They're embedded in the company profile for reads (`GET /company/info/get` returns `offices[]`) and written as a full `offices[]` replacement array on `POST /company/info/add` / `PUT /company/info/update` (diffed server-side to add/remove/update). A one-field edit on a single office means resending the full array. |
| Country validation | Free text, like `BranchEntity.Country` — no FK to the `countries` reference table |
| **Route prefix (revised 2026-06-26)** | `api/v1/company/info` (not bare `api/v1/company`) — explicit and leaves room for other `company/*` resources later without colliding with this one |
| **Logo/banner write shape (revised 2026-06-26)** | Same as employee `identity.profile_url` exactly, via the shared `ProfileUrlWriteJsonConverter`: accepts a plain document id string **or** the read-shaped object (`doc_id`/`id`/`presigned_url`), so a client can resend whatever `GET /get` returned without extracting the id itself. |

## Data model (tvs-sqlscript, branch `dev`)

New entities in `Entities/ZelosHrEntities.cs`, configs in `Configurations/ZelosHrConfigurations.cs`,
one EF migration (`dotnet ef migrations add AddZhrCompanyProfileAndOffices`).

### `ZhrCompanyProfile` → `zeloshr.zhr_company_profile`

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | `gen_random_uuid()` |
| `tenant_id` | varchar(128) | |
| `org_id` | varchar(128) | |
| `legal_name` | varchar(200) | required |
| `trading_name` | varchar(200) | nullable |
| `industry` | varchar(150) | nullable, free text |
| `company_size` | varchar(50) | nullable, free text (e.g. `"201-500 employees"`) |
| `business_registration_number` | varchar(100) | nullable |
| `tin` | varchar(100) | nullable |
| `primary_work_country` | varchar(100) | nullable, free text |
| `company_email` | varchar(200) | nullable, validated with `[EmailAddress]` (same convention as `EmployeesWriteDto.WorkEmail`) |
| `website` | varchar(300) | nullable |
| `logo_document_id` | varchar(200) | nullable — File Management registry id |
| `banner_document_id` | varchar(200) | nullable — File Management registry id |
| `created_at` / `updated_at` | timestamptz | `NOW()` default |
| `created_by` / `updated_by` | text | platform user id |

Indexes: **unique** `(tenant_id, org_id)` — enforces the 0/1-per-org cardinality at the DB level
(defense in depth alongside the app-level 409 check on `POST /add`).

### `ZhrCompanyOffice` → `zeloshr.zhr_company_offices`

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | `gen_random_uuid()` |
| `tenant_id` | varchar(128) | |
| `org_id` | varchar(128) | |
| `name` | varchar(150) | required |
| `country` | varchar(100) | nullable |
| `city` | varchar(100) | nullable |
| `phone` | varchar(50) | nullable |
| `is_head_office` | boolean | default `false`, unenforced uniqueness |
| `created_at` / `updated_at` | timestamptz | `NOW()` default |
| `created_by` / `updated_by` | text | platform user id |

Indexes: unique `(tenant_id, org_id, name)` (matches branches/id-card-types name-uniqueness
convention); index on `(tenant_id, org_id)`.

No FK between the two tables — both are scoped by `tenant_id`+`org_id` directly, consistent with
every other resource in this codebase (branches, departments, leave types, etc. have no "company"
parent row either; tenant+org scoping already implies the org).

## API surface (ZelosHR.Api)

New folder `app/src/Entities/company_info/` (snake_case folder matching route, same convention as
`id_card_types`, `employment_types`) with `CompanyInfoDtos.cs`, `CompanyInfoController.cs`,
`CompanyInfoService.cs` — a single controller/service pair. New persistence entities
`CompanyProfileEntity` / `CompanyOfficeEntity`, repositories `ICompanyProfileRepository` /
`ICompanyOfficeRepository` (+ implementations) since they're still two separate tables —
registered in `PersistenceRegistration.cs`.

All responses use the `Respons<T>` envelope and include the 6 standard audit fields
(`created_at`, `updated_at`, `created_by_id`, `updated_by_id`, `created_by`, `updated_by`) per
`docs/AUDIT_FIELDS.md`, **including each item inside the embedded `offices[]` array**.

### Company profile — `api/v1/company/info`

| Method | Route | Permission | Behavior |
|---|---|---|---|
| `GET` | `/get` | `EmployeeGet` | Fetch the profile scoped to current tenant/org, **with `offices[]` embedded** (every office for the org — no pagination, no separate list call). `404` if no profile exists. |
| `POST` | `/add` | `EmployeeUpdate` | Create the profile. Body may include an optional `offices` array to seed initial offices in the same call. `400` validation error if a profile already exists for this org (point to `PUT /update`). |
| `PUT` | `/update` | `EmployeeUpdate` | **Same shape as `POST /add`, plus `id`.** Full replacement, not a partial patch — omitted optional fields are cleared. `id` must match the profile's current id (from `GET /get`); mismatch → `400`. Optional `offices` array; see "Offices write semantics" below. `404` if not created yet. |
| `DELETE` | `/delete` | `EmployeeUpdate` | Deletes the profile **and all offices for that org**, in one transaction. `404` if no profile exists. |

Full paths: `api/v1/company/info/get`, `.../add`, `.../update`, `.../delete`. There is no
office-specific route of any kind.

#### Offices write semantics on `POST /add` / `PUT /update`

The `offices` field on these two routes is a **full-replacement array**, diffed server-side
against the org's current offices in one transaction:

- **Key absent from the body** → offices are left untouched entirely (the one field on `PUT
  /update` that is *not* full-replace-by-omission, unlike every other field).
- **Key present as `[]`** → every existing office for the org is deleted.
- **Key present as `[...]`** → for each entry:
  - No `id` (or `id` null/absent) → create a new office.
  - `id` matches an existing office for this org → that office's fields are replaced with the
    entry's values (full replace per item — every entry must carry its complete desired state).
  - `id` doesn't match any office owned by this org → `400` validation error.
  - Any existing office **not** present in the array (by id) → deleted.

A one-field edit on a single office (e.g. fixing a phone number) means: fetch `offices[]` from
`GET /get`, mutate the one entry client-side, and resend the full array on `PUT /update`. There is
no dedicated single-office route — confirmed acceptable given offices are edited by a small number
of admins on an infrequently-touched settings page.

`logo_url` / `banner_url` follow the exact `identity.profile_url` convention, including the
write-side flexibility:
- **Write** (`POST /add`, `PUT /update`): `[JsonConverter(typeof(ProfileUrlWriteJsonConverter))]`
  on a `string?` property — accepts either a plain registry id from
  `POST /api/v1/file/post/multiple`, or the read-shaped object (`doc_id` / `id` / `presigned_url`)
  so a client can resend whatever `GET /get` returned unmodified. Empty string `""` clears it.
  Validated via `HrDocumentPresignedUrlService.ValidateDocumentReferenceAsync`.
- **Read** (`GET /get`): resolved to `DocumentReadDto` (`doc_id`, `name`, `presigned_url`,
  `description`) via `HrDocumentPresignedUrlService.ResolveDocumentReadAsync`, or `null` if unset —
  identical shape to employee `identity.profile_url` / `photo_url`.

DB columns stay `logo_document_id` / `banner_document_id` (internal storage only). No new upload
endpoint.

Office validation (applies on create and on every full-replace entry): `name` required (≤150
chars, unique per org); `country`/`city`/`phone` optional free text; `is_head_office` optional
bool (default `false`).

## Cross-repo workflow (per `AGENTS.md`)

1. **`tvs-sqlscript` (branch `dev`)**: add `ZhrCompanyProfile`/`ZhrCompanyOffice` entities +
   Fluent configs, `dotnet ef migrations add AddZhrCompanyProfileAndOffices`, `dotnet build`, PR
   to `dev`, merge first, wait for `saas-dev` deploy.
2. **`ZelosHR.Api` (branch `dev`)**: entities/repositories/services/controllers/DTOs as above,
   `PersistenceRegistration.cs` DI entries, Swagger sync (new
   `SwaggerCompanyInfoOperationFilter` mirroring `SwaggerIdCardTypesOperationFilter`, examples in
   `SwaggerExamples`/`SwaggerSchemaExamplesFilter`, controller XML docs, `SwaggerConfiguration`
   workflow text), tests (service + controller tests mirroring
   `EmploymentTypesServiceTests`; extend `SwaggerGenerationTests` to assert the new paths,
   matching the existing id-card-types/employment-types assertion).
3. Verify locally: `./scripts/compose.sh migrate && ./scripts/compose.sh test`, confirm
   `/swagger` and `GET /api/v1/navigation` agree on the new routes.

## Out of scope

- Country dropdown validated against the `countries` reference table
- Enforcing exactly one head office (confirmed: client-managed, unenforced)
- New file-upload endpoints (reusing the existing File Management module as-is)
- Office street address field (not in the mockup)
- Dedicated RBAC permissions for Company Settings (reusing `EmployeeGet`/`EmployeeUpdate`)
- Standalone office endpoints of any kind, including a single-office update route, and any
  pagination/search/sort over offices (confirmed 2026-06-26: offices are always returned in full,
  embedded on `GET /company/info/get`, and written in full on `POST /add` / `PUT /update`)
- Optimistic concurrency control on the `offices` full-replace write. Two clients editing the
  array at the same time can clobber each other (last write wins) — accepted given offices are
  edited by a small number of admins on an infrequently-touched settings page. Revisit if this
  becomes a real problem.
