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
| Head-office uniqueness | **Not enforced.** `is_head_office` is a plain client-managed boolean; any number (incl. zero) of offices may be flagged; any office, head or not, can be deleted |
| Logo / banner | In scope now, via the **existing** File Management module (no new upload endpoint) |
| Delete cascade | `DELETE /company/delete` also deletes all of that org's offices, in one transaction |
| Routes / permissions | Match existing convention exactly — verb-suffix routes, reuse `EmployeeGet`/`EmployeeUpdate` permissions, no new RBAC seed |
| Office fields | `name`, `country`, `city`, `phone`, `is_head_office` only — no street address (not in mockup, YAGNI) |
| Country validation | Free text, like `BranchEntity.Country` — no FK to the `countries` reference table |

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

New folder `app/src/Entities/company/` with `CompanyDtos.cs`, `CompanyController.cs`,
`CompanyService.cs`, `CompanyOfficesController.cs`, `CompanyOfficesService.cs`. New persistence
entities `CompanyProfileEntity` / `CompanyOfficeEntity`, repositories `ICompanyProfileRepository` /
`ICompanyOfficeRepository` (+ implementations), registered in `PersistenceRegistration.cs`.

All responses use the `Respons<T>` envelope and include the 6 standard audit fields
(`created_at`, `updated_at`, `created_by_id`, `updated_by_id`, `created_by`, `updated_by`) per
`docs/AUDIT_FIELDS.md`. Query params follow snake_case via `[FromQuery(Name = ...)]` per
`docs/API_QUERY_PARAMS.md`. New constant: `PlatformQueryParams.OfficeId = "office_id"`.

### Company profile — `api/v1/company`

| Method | Route | Permission | Behavior |
|---|---|---|---|
| `GET` | `/get` | `EmployeeGet` | Fetch the profile scoped to current tenant/org. `404` if none exists. |
| `POST` | `/add` | `EmployeeUpdate` | Create the profile. `400` validation error if one already exists for this org (point to `PUT /update`). |
| `PUT` | `/update` | `EmployeeUpdate` | Partial body — only supplied fields change (same convention as `UpdateIdCardTypeDto`). `404` if not created yet. |
| `DELETE` | `/delete` | `EmployeeUpdate` | Deletes the profile **and all offices for that org**, in one transaction. `404` if no profile exists. |

`logo_document_id` / `banner_document_id` on write: accept a registry id from
`POST /api/v1/file/post/multiple` (reuse `HrDocumentPresignedUrlService.ValidateDocumentReferenceAsync`
for validation, same as `identity.profile_url`). On read, resolve to `DocumentReadDto`
(`doc_id`, `name`, `presigned_url`, `description`) via
`HrDocumentPresignedUrlService.ResolveDocumentReadAsync` — no new upload endpoint.

### Offices — `api/v1/company/offices`

| Method | Route | Permission | Behavior |
|---|---|---|---|
| `GET` | `/list` | `EmployeeGet` | Paginated; `search` (name), `sort_by` (`name`\|`country`\|`created_at`), `sort_order`, `page`, `size` — same shape as `IdCardTypeListQuery`. |
| `GET` | `/get?office_id=` | `EmployeeGet` | `404` if not found. |
| `POST` | `/add` | `EmployeeUpdate` | Create an office for the current org. |
| `PUT` | `/update?office_id=` | `EmployeeUpdate` | Partial body. `404` if not found. |
| `DELETE` | `/delete?office_id=` | `EmployeeUpdate` | No special protection — any office, including a head office, can be deleted. `404` if not found. |

Validation: `name` required (≤150 chars, unique per org); `country`/`city`/`phone` optional
free text; `is_head_office` optional bool (default `false`).

## Cross-repo workflow (per `AGENTS.md`)

1. **`tvs-sqlscript` (branch `dev`)**: add `ZhrCompanyProfile`/`ZhrCompanyOffice` entities +
   Fluent configs, `dotnet ef migrations add AddZhrCompanyProfileAndOffices`, `dotnet build`, PR
   to `dev`, merge first, wait for `saas-dev` deploy.
2. **`ZelosHR.Api` (branch `dev`)**: entities/repositories/services/controllers/DTOs as above,
   `PersistenceRegistration.cs` DI entries, Swagger sync (new
   `SwaggerCompanyOperationFilter` mirroring `SwaggerIdCardTypesOperationFilter`, examples in
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
