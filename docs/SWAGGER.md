# Swagger / OpenAPI

**UI:** http://localhost:8000/swagger  
**Spec:** http://localhost:8000/swagger/v1/swagger.json

Configuration lives in `app/src/Configs/SwaggerConfiguration.cs`.

**Platform JSON:** responses use **snake_case** (`status_code`, `has_next`, `field_errors`) to match Mystoreguard — see [MYSTOREGUARD_API_CONFORMANCE.md](MYSTOREGUARD_API_CONFORMANCE.md).

**Audit fields (all modules):** every resource item in list/get/mutation responses includes the six standard fields — see [AUDIT_FIELDS.md](AUDIT_FIELDS.md).

**Package:** `Swashbuckle.AspNetCore` **10.x** (required for .NET 10 — older 6.x produces an empty `paths` object).

## Required headers (Trove standard)

Every `/api/v1/*` request must include these headers — **exact names** (lowercase with hyphen):

| Header | Value |
|--------|--------|
| `app-id` | `app-zeloshr` |
| `authorization` | `Bearer <JWT>` |
| `bus-id` | Business id from platform context |
| `loc-id` | Location id from platform context |
| `org-id` | Organisation id |

Enforced by `TroveRequestHeadersMiddleware` when `TrovesuiteIntegration:RequireStandardHeaders` is `true` (default).

Tenant scope is taken from the JWT claim `tenant_id` (read without DB validation when `RequireAuthentication` is `false`).

`/api/v1/health` is exempt.

## What Swagger includes

**Current sprint scope:** **Employees**, **Custom Fields**, **File Management**, **Currencies**, **Organisation** (org chart), **Leave**, **Lifecycle Events**, and **Audit Logs** appear in Swagger. Other controllers stay in the codebase with `[ApiExplorerSettings(IgnoreApi = true)]` and are excluded via `SwaggerGroups.VisibleInSwagger` — remove `IgnoreApi` and add the group to that set when a module ships.

| Feature | Description |
|---------|-------------|
| **Tags** | Employees · Custom Fields · File Management · Currencies · Organisation · Leave · Lifecycle Events · Audit Logs (active); other modules hidden |
| **File Management** | Upload / list / delete — see [FILE_MANAGEMENT.md](FILE_MANAGEMENT.md) for `DocumentReadDto` vs write `document_ids` |
| **Bearer JWT** | Authorize — sets `authorization: Bearer …` |
| **Trove headers** | `app-id`, `bus-id`, `loc-id`, `org-id` on each operation (pre-filled for local demo) |
| **Standard errors** | 400, 401, 404, 409, 500 |

### Organisation org chart (`GET /api/v1/org-structure/chart`)

Response is a **reporting-line tree** (`data.roots[]`), not a department hierarchy:

| Field | Meaning |
|-------|---------|
| `full_name`, `job_title` | Person shown on each node |
| `profile_url` | `null` or `DocumentReadDto` (`doc_id`, `name`, `presigned_url`, `description`) — same as GET /employees/list |
| `node_type` | Always `employee` |
| `parent_id` | Manager employee UUID; `null` on roots |
| `department` | Badge on department heads only: `name`, `employee_count`, `headcount_capacity` (headcount bar e.g. 8/10) |
| `children` | Direct reports (nested sub-levels) |

Tree shape comes from employee `reports_to_id`. Department badge requires `head_of_department_id` on the department row.

### Leave Management (`/api/v1/leave/*`)

| Area | Key routes |
|------|------------|
| Dashboard | `GET /leave/summary` (KPIs + widgets — **use for Leave Management page**) · `GET /leave/statistics` (counts only) · `GET /leave/dashboard` (alias of summary widgets) |
| Leave Calendar | `GET /leave/calendar` — employee rows with `leave_bars[]` for Approved/Pending leave in the visible date window |
| Admin requests | `GET /leave/requests/list` · `GET /leave/requests/get` · `POST /requests/add` · `POST /requests/approve` · `POST /requests/reject` |
| Leave Approvals | `GET /leave/approvals/list` — flat employee rows for Approvals table (see below) |
| My Leave | `GET /leave/my/summary?employee_id=` · `GET /leave/my/requests/list?employee_id=` · `GET /leave/my/balances/list?employee_id=` · `POST /leave/my/requests/add?employee_id=` |
| Balances | `GET /leave/balances/list` · admin CRUD on `/leave/balances/*` |
| Settings | Leave types `/leave/types/*` · public holidays `/leave/holidays/*` |

### Public holidays (`/api/v1/leave/holidays/*`)

| Action | Endpoint |
|--------|----------|
| List | `GET /leave/holidays/list?search=&year=&country=&page=&size=` |
| View | `GET /leave/holidays/get?holiday_id=` |
| Create | `POST /leave/holidays/add` |
| Update | `PUT /leave/holidays/update?holiday_id=` |
| Delete | `DELETE /leave/holidays/delete?holiday_id=` |

Country-based holidays for leave working-day calculations. Standard audit fields on every item.

**List params** (matches frontend `PublicHolidayParams`):

| Param | Type | Meaning |
|-------|------|---------|
| `search` | string | Partial match on `holiday_name` |
| `year` | number | Calendar year (e.g. `2026`). Omit for no year filter; sets `occurrence_date` on recurring rows when set |
| `country` | string | Country name from `GET /countries/list` (e.g. `Ghana`) |
| `page` | number | Page index |
| `size` | number | Page size |

**Country workflow:**

1. `GET /countries/list` → pick `name` from response
2. `POST /leave/holidays/add` with `country` set to that name (e.g. `Ghana`)
3. Reads return `country` (display name only)
4. `PUT /leave/holidays/update?holiday_id=` — same body as POST

**Add body** (`POST /leave/holidays/add` — matches frontend `AddPublicHolidayRequest`): `holiday_name` · `date` · `is_recurring_annually` · `country`

**Recurring annually (`is_recurring_annually: true`)** — stores **one** database row with an anchor month/day on `date`. The API does **not** insert a new row each year. Leave working-day calculations and `GET /leave/holidays/list?year=2026` project that anchor into the requested calendar year (`occurrence_date` when `year` is set).

### Countries (`/api/v1/countries/*`)

| Action | Endpoint |
|--------|----------|
| List | `GET /countries/list` |
| Get | `GET /countries/get?country_id=` |

Returns `id`, `name`, `code` (ISO alpha-2). Use `id` as `country` on holidays.

**Leave types table** (`GET /leave/types/list?page=&size=`) — paginated `data.items[]` with audit fields on every row. Actions:

| UI action | Endpoint |
|-----------|----------|
| View | `GET /leave/types/get?leave_type_id=` |
| Edit | `PUT /leave/types/update?leave_type_id=` (same body as create) |
| Archive | `POST /leave/types/archive?leave_type_id=` |
| Delete | `DELETE /leave/types/delete?leave_type_id=` (409 when referenced) |

List responses use **`data.items[]`** (not `requests[]`). Rows include nested **`employee`** (`employee_id`, `full_name`, `job_title`, `profile_url`) and **`leave_type`** (`leave_type_id`, `name`). Request rows expose **`approver`** (`approver_id`, `full_name`) when decided.

Every leave resource item (requests, balances, types, holidays) includes standard audit fields per [AUDIT_FIELDS.md](AUDIT_FIELDS.md).

**List filters** (`GET /leave/requests/list`) — map UI controls to query params (combine with AND):

| UI control | Query param |
|------------|---------------|
| Employee name (search box) | `search` |
| Employee code / ID | `employee_code` or `employee_id` |
| Leave request UUID | `leave_request_id` |
| Department | `department_id` |
| Branch | `branch_id` |
| Leave type | `leave_type_id` |
| Status | `status` |
| Approval stage | `approval_stage` |
| Leave date range | `from_date` + `to_date` |
| Submitted date range | `submitted_from_date` + `submitted_to_date` |

Full table: [API_CONTRACTS.md — Leave](API_CONTRACTS.md#leave-apiv1leave).

**Leave Approvals table** (`GET /leave/approvals/list?tab=pending&page=&size=`) — flat `data.items[]` (not nested employee/leave_type objects):

| UI column | JSON field |
|-----------|------------|
| Employee | `employee_name` · `employee_code` · `title` · `profile_url` |
| Leave type | `leave_type` (string) |
| Dates | `leave_from` · `leave_to` |
| Days | `leave_days` |
| Waiting | `waiting` (hours since final queue) |
| Approved by | `approved_by[]` — each `{ name, profile_url }` (LM → HOD) |
| Badge | `pending_count` (final-stage pending total) |

Tabs: `tab=pending|history`. Sort: `sort_by=name` · `sort_order=asc|desc`. Filters: `search` · `department_id` · `leave_type_id` · `from_date` · `to_date`. Each row includes standard audit fields.

**Dashboard** (`GET /leave/summary` or `GET /leave/dashboard`) — flat employee lists per widget:

| KPI card | JSON field |
|----------|------------|
| On leave today | `summary.on_leave_today` |
| Pending approvals | `summary.pending_approvals` |
| Leaving this week | `summary.leaving_this_week` |
| Low balance alert | `summary.low_balance_alert` |

| Widget | Row fields |
|--------|------------|
| `on_leave_today[]` | `employee_name` · `profile_url` · `leave_type` · `returns_on` |
| `pending_approvals[]` | `employee_name` · `profile_url` · `leave_type` · `leave_days` · `waiting` (“waiting 53h”) **or** `hours_since_last_approval` (“approved 12h”) **or** `days_since_last_approval` (“approved 5d”) |
| `leaving_this_week[]` | `starts_on` · `employee_name` · `profile_url` · `leave_type` · `leave_days` |

Each row includes `leave_request_id` and `employee_id` for navigation / approve actions, plus standard audit fields (`created_at`, `updated_at`, `created_by_id`, `updated_by_id`, `created_by`, `updated_by`). `pending_approvals` lists **all** pending requests (any approval stage), oldest `submitted_at` first.

**Leave Calendar** (`GET /leave/calendar?view=week|month&anchor_date=&page=&size=`) — flat rows (employee + leave range on the same level):

| Field | Description |
|-------|-------------|
| `view` | `week` (Mon–Sun) or `month` — sets window from `anchor_date` when dates omitted |
| `anchor_date` | Date inside the week/month to show (default today UTC) |
| `from_date` / `to_date` | Explicit window (overrides `view` when both set) |
| `items[]` | Flat rows: employee fields + `leave_from` · `leave_to` · `leave_type` · `status` · `leave_days` |
| No leave | One row per employee with null `leave_request_id` / leave fields |
| Multiple leaves | Same `employee_id` repeated — one row per Approved/Pending range |

Filters: `search` (min 2 chars) · `department_id` · `leave_type_id`. Pagination counts **employees** (page may return more than `size` rows when employees have multiple leaves).

**Balances** (`GET /leave/my/summary?employee_id=`, `/leave/my/balances/list?employee_id=`, `/leave/balances/*`) use the same nested ref pattern on each balance row.

Three-stage approval: `line_manager` → `head_of_department` → `final`. Final queue: `?approval_stage=pending_final&status=Pending`. Mutations and `GET /requests/get` return **`LeaveRequestDetailDto`** with `balance_impact` and `approval_trail`.

### Audit logs (read-only)

| Endpoint | Purpose |
|----------|---------|
| `GET /api/v1/audit-logs/statistics` | KPI cards (total, critical, flagged, sensitive reads, unique actors) |
| `GET /api/v1/audit-logs/list` | Paginated table with filters: `search`, `action`, `severity`, `actor`, `start_date`, `end_date` |
| `GET /api/v1/audit-logs/export` | CSV export (same filters as list) |
| `GET /api/v1/audit-logs/purge/preview?retention_window=` | Count entries eligible for purge (90, 180, or 365 days) |
| `DELETE /api/v1/audit-logs/purge?retention_window=` | Delete entries older than retention window (days) |
| `GET /api/v1/audit-logs/get?audit_log_id=` | Single entry |
| `GET /api/v1/users/get-users` | Paginated Trovesuite users (`is_active`, `delete_status`, `can_login`, `email`, `fullname`, `gender`, `use_or`) |

Entries are appended automatically when employees are created or updated (Phase 1). `actor_id` is the platform user id from the JWT when available.

### Employee CSV export (`GET /api/v1/employees/export`)

| Query param | Purpose |
|-------------|---------|
| `start_date` | Employment start on or after (`YYYY-MM-DD`; uses `start_date` or `employment_start_date`) |
| `end_date` | Employment start on or before |
| `search`, `employment_status`, `department_id`, `branch_id`, … | Same filters as `GET /employees/list` |

Returns `text/csv` with columns aligned to bulk import plus `employee_id`, `employee_code`, `department_name`, `branch_name`, `employment_status`, `start_date`.

## Required on every API change (MUST)

Any PR that changes request/response shapes, query params, routes, or workflows **must** update Swagger in the same commit. Do not merge API code without matching OpenAPI docs.

| Touch point | File(s) |
|-------------|---------|
| New/changed DTO property | XML `<summary>` on the property; `SwaggerSchemaExamplesFilter` (type + property examples/descriptions) |
| New/changed response envelope | `SwaggerExamples` (response JSON); matching `Swagger*OperationFilter` 200 example |
| New/changed request body | `SwaggerExamples` + `SwaggerRequestExamplesOperationFilter` named examples |
| New/changed query param | `SwaggerQueryParameterExamplesFilter`; `[SwaggerAllowedValues]` where enums apply |
| New/changed route or workflow | `SwaggerConfiguration` intro text; controller XML `<remarks>`; group operation filter |
| New controller / tag | `SwaggerGroups.VisibleInSwagger`; `ApiExplorerSettings(GroupName)`; tag document filter if needed |

**Verify before merge:** `./scripts/compose.sh dev` → open `/swagger` → confirm Try it out examples match the code change; spot-check `/swagger/v1/swagger.json`.

See also PR checklist in [AGENTS.md](../AGENTS.md).

## Try it out (local)

1. Start API: `./scripts/compose.sh dev` (must run with `ASPNETCORE_ENVIRONMENT=Development`)
2. Open http://localhost:8000/swagger
3. **Development prefills automatically:**
   - **Authorize** → Bearer JWT for demo admin (`demo-tenant`, `u1000001-…`)
   - Operation headers → `app-id`, `org-id`, `bus-id`, `loc-id` (demo seed values)
   - Every **Execute** also sends headers via a request interceptor (even if a field looks empty)
4. Pick any `/api/v1/*` operation → **Try it out** → **Execute**

Optional manual token: `./scripts/gen-trovesuite-jwt.sh` → paste into **Authorize** or the `authorization` header.

Bootstrap JSON (dev only): http://localhost:8000/swagger/dev-bootstrap.json

Example `curl` (replace ids and token from your platform session):

```bash
curl -s "http://localhost:8000/api/v1/employees/directory/summary" \
  -H 'app-id: app-zeloshr' \
  -H 'authorization: Bearer <JWT>' \
  -H 'bus-id: bus_5d929457b0ea7e6d55c5da25c8cfb38aeef0573658121bf5399f6f1e64d' \
  -H 'loc-id: loc_c79fd9a5c53a8eaa82805e63a84da112387743c5dcdff7f7b254c02302c' \
  -H 'org-id: org_bcf5a0951f5ed22448dc5262e641e428caa3638d38b94cfa3b79c13d38a'
```

Legacy `X-Tenant-Id` / `X-Org-Id` still work only when `RequireStandardHeaders` is `false` (not recommended).

## Local header values (Swagger prefill)

In Development, Swagger prefills Trove-style ids from `LocalDevelopment` (same shape as production curl). Values must exist in **your** database (`cp_business_app_locations`, `cp_user_locations`, etc.) — add rows locally or point at a shared dev instance. JWT from `./scripts/gen-trovesuite-jwt.sh` should use the same `tenant_id` / `user_id` as those rows:

| Header | Value |
|--------|--------|
| `app-id` | `app-zeloshr` |
| `org-id` | `org_bcf5a0951f5ed22448dc5262e641e428caa3638d38b94cfa3b79c13d38a` |
| `bus-id` | `bus_5d929457b0ea7e6d55c5da25c8cfb38aeef0573658121bf5399f6f1e64d` |
| `loc-id` | `loc_c79fd9a5c53a8eaa82805e63a84da112387743c5dcdff7f7b254c02302c` |

Wrong `bus-id` / `loc-id` / `org-id` returns **403** when `ValidatePlatformContext` is enabled (checks `cp_business_app_locations` and `cp_user_locations`).

## Production (Container App)

Swagger is enabled in **Production** at `/swagger` (same spec as local). The OpenAPI document is generated at request time from the running assembly — it is not a static file checked into git.

If production looks **out of date** compared to local:

1. **Confirm the running build** — open `/swagger` and check the title or `info.version` in `/swagger/v1/swagger.json` for `build <git-sha>`. Compare with the latest successful **Build & Deploy** workflow on `dev`/`main`.
2. **Hard-refresh** the browser (spec responses use `Cache-Control: no-store`; shift+reload on `/swagger`).
3. **Confirm the new image is live** — `GET /health` includes `build` (same `BUILD_VERSION` env from CI). After deploy, revision must be healthy (not crash-looping on config).
4. **Compare route counts** — `GET /api/v1/navigation` and `/swagger/v1/swagger.json` should list the same endpoints (~55 paths). If `paths` is empty, the image was built without **Swashbuckle.AspNetCore 10.x** (see top of this doc).

Production does **not** prefill JWT/headers (Development only). Use a real Trove Bearer token and platform `org-id` / `bus-id` / `loc-id` in **Authorize** / **Try it out**.

## Related docs

- [FILE_MANAGEMENT.md](FILE_MANAGEMENT.md) — employee documents, uploads, MyStoreGuard shapes
- [API_CONTRACTS.md](API_CONTRACTS.md) — frontend field reference
- [MYSTOREGUARD_API_CONFORMANCE.md](MYSTOREGUARD_API_CONFORMANCE.md) — platform parity
