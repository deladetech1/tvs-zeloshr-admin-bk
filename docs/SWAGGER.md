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

**Current sprint scope:** **Employees**, **Custom Fields**, **File Management**, **Currencies**, and **Organisation** (org chart) appear in Swagger. Other controllers stay in the codebase with `[ApiExplorerSettings(IgnoreApi = true)]` and are excluded via `SwaggerGroups.VisibleInSwagger` — remove `IgnoreApi` and add the group to that set when a module ships.

| Feature | Description |
|---------|-------------|
| **Tags** | Employees · Custom Fields · File Management · Currencies · Organisation (active); other modules hidden |
| **Bearer JWT** | Authorize — sets `authorization: Bearer …` |
| **Trove headers** | `app-id`, `bus-id`, `loc-id`, `org-id` on each operation (pre-filled for local demo) |
| **Standard errors** | 400, 401, 404, 409, 500 |

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
