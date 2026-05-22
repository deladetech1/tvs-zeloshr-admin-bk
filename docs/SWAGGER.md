# Swagger / OpenAPI

**UI:** http://localhost:8000/swagger  
**Spec:** http://localhost:8000/swagger/v1/swagger.json

Configuration lives in `app/src/Configs/SwaggerConfiguration.cs`.

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

| Feature | Description |
|---------|-------------|
| **Tags** | One group per HR module |
| **Bearer JWT** | Authorize — sets `authorization: Bearer …` |
| **Trove headers** | `app-id`, `bus-id`, `loc-id`, `org-id` on each operation (pre-filled for local demo) |
| **Standard errors** | 400, 401, 404, 409, 500 |

## Try it out (local)

1. Start API: `./scripts/compose.sh dev`
2. Generate a dev JWT: `./scripts/gen-trovesuite-jwt.sh`
3. Open http://localhost:8000/swagger
4. **Authorize** → paste `Bearer <token>` (Swagger adds the `Bearer ` prefix if you paste only the token, use full `Bearer …` in the header field if needed)
5. On any operation, confirm headers: `app-id=app-hr`, `bus-id`, `loc-id`, `org-id` (defaults shown in Try it out)
6. Execute

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
