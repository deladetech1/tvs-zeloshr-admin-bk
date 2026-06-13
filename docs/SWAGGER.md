# Swagger / OpenAPI

**UI:** http://localhost:8000/swagger  
**Spec:** http://localhost:8000/swagger/v1/swagger.json

Configuration lives in `app/src/Configs/SwaggerConfiguration.cs`.

**Platform JSON:** responses use **snake_case** (`status_code`, `has_next`, `field_errors`) to match Mystoreguard — see [MYSTOREGUARD_API_CONFORMANCE.md](MYSTOREGUARD_API_CONFORMANCE.md).

**Package:** `Swashbuckle.AspNetCore` **10.x** (required for .NET 10 — older 6.x produces an empty `paths` object).

## Required headers (Trove standard)

Every `/api/v1/*` request must include these headers — **exact names** (lowercase with hyphen):

| Header | Value |
|--------|--------|
| `app-id` | `app-hr` |
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
| Dashboard | `GET /leave/statistics` (KPI cards) · `GET /leave/dashboard` (widgets) |
| Admin requests | `GET /leave/requests/list` · `GET /leave/requests/get` · `POST /requests/add` · `POST /requests/approve` · `POST /requests/reject` |
| My Leave | `GET /leave/my/summary` · `GET /leave/my/requests/list` · `POST /leave/my/requests/add` |
| Balances | `GET /leave/balances/list` · admin CRUD on `/leave/balances/*` |
| Settings | Leave types `/leave/types/*` · public holidays `/leave/holidays/*` |

List responses use **`data.items[]`** (not `requests[]`). Rows include nested **`employee`** (`full_name`, `job_title`, `profile_url`) and **`leave_type`** (`name`) for display — keep flat `employee_id` / `leave_type_id` for filters and forms only.

**Balances** (`GET /leave/my/summary`, `/leave/my/balances/list`, `/leave/balances/*`) use the same nested ref pattern on each balance row.

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
  -H 'app-id: app-hr' \
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
| `app-id` | `app-hr` |
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
